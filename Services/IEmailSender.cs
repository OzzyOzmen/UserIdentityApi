using System.Threading.Tasks;

namespace UserIdentityApi.Services
{
    public interface IEmailSender
    {
       Task SendEmailAsync(string emailAddress, string subject, string content);
    }
}