using WebAPI.Controllers.Helpers;

namespace WebAPI.Constants
{
    /// <summary>
    /// Error codes.
    /// </summary>
    public enum ErrorCode : int
    {
        [StringValue("Missing mandatory field in the request")]
        MissingMandatoryField = 100,

        [StringValue("User already exists")]
        UserAlreadyExists = 101,

        [StringValue("Invalid User Role Id")]
        InvalidUserRoleId = 102,

        [StringValue("User not found")]
        UserNotFound = 103,

        [StringValue("User not allowed")]
        UserNotAllowed = 104,

        [StringValue("Invalid User Status Id")]
        InvalidUserStatusId = 105,

        [StringValue("Invalid Email")]
        InvalidEmail = 106,

        [StringValue("Negative value")]
        NegativeValue = 107,

        [StringValue("Invalid product Id")]
        InvalidProductId = 108,

        [StringValue("Invalid Request status Id")]
        InvalidRequestStatusId = 109,

        [StringValue("Invalid Request RefNo")]
        InvalidRefNo = 110,

        [StringValue("Not implemented")]
        NotImplemented = 111,

        [StringValue("Unable to send B2B invitation")]
        UnableToSendB2BInvitation = 111,

        [StringValue("Bot validation error")]
        BotValidationError = 112,
    }
}
