namespace ZelosHR.Api.Entities.EmployeePortal;

internal static class EmployeePortalPasswordResetTemplates
{
    internal const string TextTemplate =
        """
        Hello {first_name},

        We received a request to reset the password for your {company_short_name} employee portal account.

        Reset your password using the link below:

        {reset_url}

        This link expires in {expiry_hours} hour(s) and can only be used once.

        If you did not request this, you can ignore this email.

        {cdate} at {ctime}
        """;

    internal const string HtmlTemplate =
        """
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><title>Reset your Zelos HR password</title></head>
        <body style="margin:0;padding:0;background:#f4f6f8;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f6f8;padding:24px 0;">
            <tr>
              <td align="center">
                <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="background:#ffffff;border-radius:8px;overflow:hidden;">
                  <tr>
                    <td style="background:#0f766e;padding:24px 32px;color:#ffffff;font-size:20px;font-weight:bold;">
                      {company_name}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:32px;">
                      <p style="margin:0 0 16px;font-size:16px;">Hello {first_name},</p>
                      <p style="margin:0 0 16px;font-size:15px;line-height:1.6;">
                        We received a request to reset the password for your <strong>{company_short_name}</strong> employee portal account.
                      </p>
                      <p style="margin:24px 0;text-align:center;">
                        <a href="{reset_url}" style="display:inline-block;background:#0f766e;color:#ffffff;text-decoration:none;padding:14px 28px;border-radius:6px;font-weight:bold;">
                          Reset your password
                        </a>
                      </p>
                      <p style="margin:0 0 8px;font-size:14px;color:#4b5563;">
                        Or copy this link into your browser:<br/>
                        <a href="{reset_url}" style="color:#0f766e;word-break:break-all;">{reset_url}</a>
                      </p>
                      <p style="margin:16px 0 0;font-size:13px;color:#6b7280;">
                        This link expires in {expiry_hours} hour(s) and can only be used once.
                      </p>
                      <p style="margin:16px 0 0;font-size:13px;color:#6b7280;">
                        If you did not request a password reset, you can safely ignore this email.
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:16px 32px;background:#f9fafb;font-size:12px;color:#9ca3af;text-align:center;">
                      Powered by TroveSuite · Zelos HR
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
