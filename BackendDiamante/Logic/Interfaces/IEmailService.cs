namespace BackendDiamante.Logic.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
}
