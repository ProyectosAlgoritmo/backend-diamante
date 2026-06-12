using System.Net;
using System.Net.Mail;
using BackendDiamante.Logic.Interfaces;

namespace BackendDiamante.Logic;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
    {
        var smtpHost     = _config["Email:SmtpHost"]!;
        var smtpPort     = _config.GetValue<int>("Email:SmtpPort");
        var smtpUser     = _config["Email:SmtpUser"]!;
        var smtpPassword = _config["Email:SmtpPassword"]!;
        var fromEmail    = _config["Email:FromEmail"]!;
        var fromName     = _config["Email:FromName"] ?? "Diamante - Limpieza Inteligente";

        var htmlBody = BuildPasswordResetHtml(userName, resetLink);

        using var message = new MailMessage
        {
            From       = new MailAddress(fromEmail, fromName),
            Subject    = "Recuperación de contraseña - Diamante",
            Body       = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(new MailAddress(toEmail, userName));

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials  = new NetworkCredential(smtpUser, smtpPassword),
            EnableSsl    = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Correo de recuperación enviado a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo de recuperación a {Email}", toEmail);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName, string username, string password)
    {
        var smtpHost     = _config["Email:SmtpHost"]!;
        var smtpPort     = _config.GetValue<int>("Email:SmtpPort");
        var smtpUser     = _config["Email:SmtpUser"]!;
        var smtpPassword = _config["Email:SmtpPassword"]!;
        var fromEmail    = _config["Email:FromEmail"]!;
        var fromName     = _config["Email:FromName"] ?? "Diamante - Limpieza Inteligente";

        var htmlBody = BuildWelcomeHtml(userName, toEmail, username, password);

        using var message = new MailMessage
        {
            From       = new MailAddress(fromEmail, fromName),
            Subject    = "Bienvenido a Diamante - Tus credenciales de acceso",
            Body       = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(new MailAddress(toEmail, userName));

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials  = new NetworkCredential(smtpUser, smtpPassword),
            EnableSsl    = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Correo de bienvenida enviado a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo de bienvenida a {Email}", toEmail);
            throw;
        }
    }

    // ─── Templates HTML ─────────────────────────────────────────────────────

    private static string BuildPasswordResetHtml(string userName, string resetLink)
    {
        // Extraer solo el primer nombre para el saludo
        var firstName = userName.Split(' ')[0];

        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Recuperación de contraseña</title>
</head>
<body style=""margin:0; padding:0; background-color:#f4f6f9; font-family:'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f6f9; padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff; border-radius:12px; box-shadow:0 2px 12px rgba(0,0,0,0.08); overflow:hidden; max-width:560px; width:100%;"">

          <!-- Header -->
          <tr>
            <td style=""background: linear-gradient(135deg, #1c3fac 0%, #0056B4 100%); padding:32px 40px; text-align:center;"">
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin:0 auto;"">
                <tr>
                  <td style=""width:36px; height:36px; background:#ffffff; border-radius:6px; transform:rotate(45deg); text-align:center; vertical-align:middle;"">
                    <span style=""display:inline-block; transform:rotate(-45deg); color:#1c3fac; font-size:7px; font-weight:800; letter-spacing:1px;"">&#9670;</span>
                  </td>
                  <td style=""padding-left:14px;"">
                    <span style=""color:#ffffff; font-size:20px; font-weight:700; letter-spacing:0.5px;"">DIAMANTE</span>
                  </td>
                </tr>
              </table>
              <p style=""color:rgba(255,255,255,0.85); font-size:13px; margin:10px 0 0; font-weight:400;"">Limpieza Inteligente</p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:36px 40px 20px;"">
              <!-- Icon -->
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td style=""width:48px; height:48px; background:#dbeafe; border-radius:12px; text-align:center; vertical-align:middle;"">
                    <span style=""font-size:22px;"">&#128274;</span>
                  </td>
                </tr>
              </table>

              <h1 style=""color:#1a1a2e; font-size:22px; font-weight:700; margin:20px 0 8px;"">Hola {firstName}</h1>
              <p style=""color:#4a5568; font-size:15px; line-height:1.7; margin:0 0 24px;"">
                Recibimos una solicitud para restablecer la contraseña de tu cuenta.
                Si realizaste esta solicitud, haz clic en el botón de abajo para continuar.
              </p>

              <!-- CTA Button -->
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                <tr>
                  <td align=""center"" style=""padding:8px 0 28px;"">
                    <!--[if mso]>
                    <v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" href=""{resetLink}"" style=""height:48px;v-text-anchor:middle;width:280px;"" arcsize=""17%"" strokecolor=""#1c3fac"" fillcolor=""#1c3fac"">
                    <center style=""color:#ffffff;font-family:'Segoe UI',sans-serif;font-size:15px;font-weight:600;"">Restablecer contraseña</center>
                    </v:roundrect>
                    <![endif]-->
                    <!--[if !mso]><!-->
                    <a href=""{resetLink}"" target=""_blank"" style=""display:inline-block; background:#1c3fac; color:#ffffff; text-decoration:none; font-size:15px; font-weight:600; padding:14px 40px; border-radius:8px; letter-spacing:0.3px;"">
                      Restablecer contraseña
                    </a>
                    <!--<![endif]-->
                  </td>
                </tr>
              </table>

              <!-- Fallback link -->
              <p style=""color:#6b7280; font-size:13px; line-height:1.6; margin:0 0 8px;"">
                Si el botón no funciona, copia y pega este enlace en tu navegador:
              </p>
              <p style=""color:#1c3fac; font-size:12px; word-break:break-all; margin:0 0 28px; padding:12px 16px; background:#f8fafc; border-radius:6px; border:1px solid #e8e8e8;"">
                {resetLink}
              </p>

              <!-- Expiration warning -->
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                <tr>
                  <td style=""padding:16px; background:#fef3c7; border-radius:8px; border-left:4px solid #f59e0b;"">
                    <p style=""color:#92400e; font-size:13px; margin:0; line-height:1.5;"">
                      <strong>&#9202; Este enlace expira en 1 hora.</strong><br/>
                      Por seguridad, solo puede utilizarse una vez.
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Security note -->
          <tr>
            <td style=""padding:0 40px 36px;"">
              <p style=""color:#9ca3af; font-size:12px; line-height:1.6; margin:20px 0 0;"">
                Si no solicitaste este cambio, ignora este correo. Tu contraseña no será modificada y tu cuenta permanecerá segura.
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#f8fafc; padding:20px 40px; border-top:1px solid #e8e8e8; text-align:center;"">
              <p style=""color:#9ca3af; font-size:11px; margin:0; line-height:1.5;"">
                &copy; {DateTime.UtcNow.Year} Diamante &mdash; Limpieza Inteligente<br/>
                Este es un correo automático, por favor no respondas a este mensaje.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    private static string BuildWelcomeHtml(string userName, string email, string username, string password)
    {
        var firstName = userName.Split(' ')[0];

        return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>Bienvenido a Diamante</title>
</head>
<body style=""margin:0; padding:0; background-color:#f4f6f9; font-family:'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f6f9; padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff; border-radius:12px; box-shadow:0 2px 12px rgba(0,0,0,0.08); overflow:hidden; max-width:560px; width:100%;"">

          <!-- Header -->
          <tr>
            <td style=""background: linear-gradient(135deg, #1c3fac 0%, #0056B4 100%); padding:32px 40px; text-align:center;"">
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin:0 auto;"">
                <tr>
                  <td style=""width:36px; height:36px; background:#ffffff; border-radius:6px; transform:rotate(45deg); text-align:center; vertical-align:middle;"">
                    <span style=""display:inline-block; transform:rotate(-45deg); color:#1c3fac; font-size:7px; font-weight:800; letter-spacing:1px;"">&#9670;</span>
                  </td>
                  <td style=""padding-left:14px;"">
                    <span style=""color:#ffffff; font-size:20px; font-weight:700; letter-spacing:0.5px;"">DIAMANTE</span>
                  </td>
                </tr>
              </table>
              <p style=""color:rgba(255,255,255,0.85); font-size:13px; margin:10px 0 0; font-weight:400;"">Limpieza Inteligente</p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:36px 40px 20px;"">
              <!-- Icon -->
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td style=""width:48px; height:48px; background:#d1fae5; border-radius:12px; text-align:center; vertical-align:middle;"">
                    <span style=""font-size:22px;"">&#127881;</span>
                  </td>
                </tr>
              </table>

              <h1 style=""color:#1a1a2e; font-size:22px; font-weight:700; margin:20px 0 8px;"">Bienvenido, {firstName}</h1>
              <p style=""color:#4a5568; font-size:15px; line-height:1.7; margin:0 0 24px;"">
                Tu cuenta ha sido creada exitosamente en la plataforma <strong>Diamante</strong>.
                A continuación encontrarás tus credenciales de acceso para iniciar sesión.
              </p>

              <!-- Credentials Card -->
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""margin-bottom:24px;"">
                <tr>
                  <td style=""background:#f0f4ff; border-radius:10px; border:1px solid #d4deff; padding:24px;"">
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                      <tr>
                        <td style=""padding-bottom:16px; border-bottom:1px solid #d4deff;"">
                          <p style=""color:#6b7280; font-size:12px; margin:0 0 4px; text-transform:uppercase; letter-spacing:0.5px; font-weight:600;"">Correo</p>
                          <p style=""color:#1a1a2e; font-size:16px; margin:0; font-weight:600; font-family:'Courier New',monospace;"">{email}</p>
                        </td>
                      </tr>
                      <tr>
                        <td style=""padding-top:16px; padding-bottom:16px; border-bottom:1px solid #d4deff;"">
                          <p style=""color:#6b7280; font-size:12px; margin:0 0 4px; text-transform:uppercase; letter-spacing:0.5px; font-weight:600;"">Usuario</p>
                          <p style=""color:#1a1a2e; font-size:16px; margin:0; font-weight:600; font-family:'Courier New',monospace;"">{username}</p>
                        </td>
                      </tr>
                      <tr>
                        <td style=""padding-top:16px;"">
                          <p style=""color:#6b7280; font-size:12px; margin:0 0 4px; text-transform:uppercase; letter-spacing:0.5px; font-weight:600;"">Contraseña inicial</p>
                          <p style=""color:#1a1a2e; font-size:16px; margin:0 4px 0; font-weight:600; font-family:'Courier New',monospace;"">{password}</p>
                          <p style=""color:#6b7280; font-size:11px; margin:0;"">Los primeros 4 dígitos de tu documento de identidad</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>

              <!-- Security warning -->
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                <tr>
                  <td style=""padding:16px; background:#fef3c7; border-radius:8px; border-left:4px solid #f59e0b;"">
                    <p style=""color:#92400e; font-size:13px; margin:0; line-height:1.5;"">
                      <strong>&#128272; Importante:</strong> Al iniciar sesión por primera vez, el sistema te pedirá cambiar tu contraseña por una nueva.
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Security note -->
          <tr>
            <td style=""padding:0 40px 36px;"">
              <p style=""color:#9ca3af; font-size:12px; line-height:1.6; margin:20px 0 0;"">
                Si no esperabas esta cuenta, por favor contacta al administrador del sistema. No compartas tus credenciales con nadie.
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#f8fafc; padding:20px 40px; border-top:1px solid #e8e8e8; text-align:center;"">
              <p style=""color:#9ca3af; font-size:11px; margin:0; line-height:1.5;"">
                &copy; {DateTime.UtcNow.Year} Diamante &mdash; Limpieza Inteligente<br/>
                Este es un correo automático, por favor no respondas a este mensaje.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
