using Page_Navigation_App.Model;
using System.Threading.Tasks;

namespace Page_Navigation_App.Services
{
    public interface INotificationService
    {
        Task SendEmail(string recipient, string subject, string body);
        Task SendSMS(string phoneNumber, string message);
        Task SendWhatsApp(string phoneNumber, string message);
        Task SendBirthdayWishes(Customer customer);
        Task SendAnniversaryWishes(Customer customer);
        Task SendNotification(string title, string message);
    }
}