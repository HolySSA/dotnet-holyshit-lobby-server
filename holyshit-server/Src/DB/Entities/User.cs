using System.ComponentModel.DataAnnotations;

namespace HolyShitServer.DB.Entities;

public class User
{
  [Key]
  public long Id { get; set; }

  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;

  [Required]
  public string Nickname { get; set; } = string.Empty;

  [Required]
  public string PasswordHash { get; set; } = string.Empty;

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime? UpdatedAt { get; set; }
}

/*
var sql = "INSERT INTO users (email, nickname, password) VALUES (@email, @nickname, @hash)";
using var cmd = new NpgsqlCommand(sql, connection);
cmd.Parameters.AddWithValue("email", email);
cmd.Parameters.AddWithValue("nickname", nickname);
cmd.Parameters.AddWithValue("hash", password);
await cmd.ExecuteNonQueryAsync();
*/
