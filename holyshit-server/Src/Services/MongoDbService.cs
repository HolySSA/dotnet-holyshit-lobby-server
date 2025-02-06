using HolyShitServer.Src.DB.Configuration;
using HolyShitServer.Src.DB.MongoEntities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace HolyShitServer.Src.Services;

public class MongoDbService
{
  private readonly IMongoCollection<ChatMessages> _chatMessages;

  public MongoDbService(IConfiguration configuration)
  {
    var mongoConfig = configuration.GetSection("MongoDb").Get<MongoDbConfig>();
    
    // MongoDB 설정 최적화
    var settings = MongoClientSettings.FromUrl(new MongoUrl(mongoConfig?.ConnectionString));
    settings.WriteConcern = WriteConcern.Unacknowledged; // 빠른 쓰기 위해
    settings.ReadPreference = ReadPreference.Primary; // 읽기 성능 향상
    settings.MaxConnectionPoolSize = 100; // 커넥션 풀 크기 증가
    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
    
    // 인덱스 생성 옵션 설정
    var client = new MongoClient(settings);
    var database = client.GetDatabase(mongoConfig?.DatabaseName);
    _chatMessages = database.GetCollection<ChatMessages>("chat_messages");

    // CreatedAt 필드에 인덱스 생성 (내림차순)
    var indexKeys = Builders<ChatMessages>.IndexKeys.Descending(x => x.CreatedAt);
    var indexOptions = new CreateIndexOptions { Background = true };
    _chatMessages.Indexes.CreateOne(new CreateIndexModel<ChatMessages>(indexKeys, indexOptions));
  }

  public async Task SaveChatMessageAsync(ChatMessages message)
  {
    await _chatMessages.InsertOneAsync(message);
  }

  public async Task<List<ChatMessages>> GetChatMessagesAsync(int limit = 100)
  {
    // CreatedAt 인덱스를 활용한 빠른 조회
    return await _chatMessages.Find(_ => true)
      .Sort(Builders<ChatMessages>.Sort.Descending(x => x.CreatedAt))
      .Limit(limit)
      .ToListAsync();
  }
}