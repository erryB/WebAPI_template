using System.Threading.Tasks;

namespace WebAPI.Services
{
    /// <summary>
    /// MS Graph Service interface.
    /// </summary>
    public interface IMSGraphService
    {
        /// <summary>
        /// Invites users to B2B tenant via email address.
        /// </summary>
        /// <param name="email">Email of the user to invite to B2B.</param>
        /// <returns>Returns "PendingAcceptance", "Completed", "InProgress", "Error" based on return value from the B2B Invite.</returns>
        Task<string> InviteUser(string email);
    }
}