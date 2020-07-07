using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace WebAPI.Services
{
    /// <summary>
    /// MS Graph Service.
    /// </summary>
    public class MSGraphService : IMSGraphService
    {
        /// <summary>
        /// Invite Pending.
        /// </summary>
        public const string INVITEPENDING = "PendingAcceptance";

        /// <summary>
        /// Invite Completed.
        /// </summary>
        public const string INVITECOMPLETED = "Completed";

        /// <summary>
        /// Invite In Progress.
        /// </summary>
        public const string INVITEINPROGRESS = "InProgress";

        /// <summary>
        /// Invite Error.
        /// </summary>
        public const string INVITEERROR = "Error";

        private readonly ITokenAcquisition tokenAcquisition;
        private readonly ILogger<MSGraphService> logger;
        private readonly string graphEndpoint = "https://graph.microsoft.com/v1.0";
        private readonly string inviteRedirectUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSGraphService"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="tokenAcquisition">Token acquisition.</param>
        /// <param name="logger">Logger.</param>
        public MSGraphService(IConfiguration configuration, ITokenAcquisition tokenAcquisition, ILogger<MSGraphService> logger)
        {
            this.tokenAcquisition = tokenAcquisition;
            this.logger = logger;

            inviteRedirectUrl = configuration.GetValue<string>("InviteLandingPage");
        }

        /// <summary>
        /// Invites users to B2B tenant via email address.
        /// </summary>
        /// <param name="email">Email of the user to invite to B2B.</param>
        /// <returns>Returns "PendingAcceptance", "Completed", "InProgress", "Error" based on return value from the B2B Invite.</returns>
        public async Task<string> InviteUser(string email)
        {
            string[] inviteScopes = { "User.Invite.All" };
            string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(inviteScopes);

            GraphServiceClient graphClient = new GraphServiceClient(
                graphEndpoint,
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                new DelegateAuthenticationProvider(async (requestMessage) =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                    requestMessage.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", accessToken)));

            var invitation = new Invitation
            {
                InvitedUserEmailAddress = email,
                InviteRedirectUrl = inviteRedirectUrl,
                SendInvitationMessage = true,
            };

            try
            {
                invitation = await graphClient.Invitations.Request().AddAsync(invitation);
            }
            catch (ServiceException ex) when (ex.Error.Message == "Invitee is in inviter tenant")
            {
                logger.LogError(ex.Message);
                return INVITECOMPLETED;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return INVITEERROR;
            }

            return invitation.Status;
        }
    }
}