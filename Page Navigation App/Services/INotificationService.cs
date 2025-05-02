using System.Threading.Tasks;

namespace Page_Navigation_App.Services
{
    public interface INotificationService
    {
        Task<bool> SendSMS(string phoneNumber, string message);
        Task<bool> SendWhatsApp(string phoneNumber, string message);
        Task<bool> SendEmail(string emailAddress, string subject, string message);
        Task<bool> SendRepairNotification(int customerId, int repairJobId, string message);
    }
}