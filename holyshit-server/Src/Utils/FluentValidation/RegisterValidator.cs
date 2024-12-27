using FluentValidation;
using HolyShitServer.Src.Network.Packets;

namespace HolyShitServer.Src.Utils.FluentValidation;

public class RegisterRequestValidator : AbstractValidator<C2SRegisterRequest>
{
  public RegisterRequestValidator()
  {
    RuleFor(x => x.Email)
        .NotEmpty().WithMessage("이메일을 입력해주세요")
        .EmailAddress().WithMessage("올바른 이메일 형식이 아닙니다");

    RuleFor(x => x.Nickname)
        .NotEmpty().WithMessage("닉네임을 입력해주세요")
        .Length(2, 20).WithMessage("닉네임은 2~20자 사이여야 합니다");

    RuleFor(x => x.Password)
        .NotEmpty().WithMessage("비밀번호를 입력해주세요")
        .MinimumLength(8).WithMessage("비밀번호는 최소 8자 이상이어야 합니다")
        .Matches(@"[a-z]").WithMessage("소문자를 포함해야 합니다")
        .Matches(@"[0-9]").WithMessage("숫자를 포함해야 합니다")
        .Matches(@"[!@#$%^&*]").WithMessage("특수문자를 포함해야 합니다");
  }
}