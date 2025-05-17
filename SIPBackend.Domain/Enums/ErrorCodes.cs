namespace SIPBackend.Domain;

public enum ErrorCodes
{
    //GeneralErrors
    ModelCreatingIsFailed = 100,
    FailedToSendMessage = 101,
    
    //AuthService
    FailedToFindUser = 200,
    FailedToChangeUser = 201,
    FailedToRegisterUser = 202,
    UserAlreadyExists = 203,
    PasswordsAreNotEqual = 204,
    FailedToLogout = 205,
    FailedToLoginUser = 206,
    UserNotExists = 207,
    IncorrectCredentials = 208,
    UserAreNotAuthorized = 209,
    FailedToGetAccountInfo = 210,
    FailedToChangeUserInfo = 211,
    FailedToResetPassword = 212,
    ResettingPasswordTokenIsInvalid = 213,
    FailedToGetUserInfo = 214,
    FailedToGetRefreshToken = 215,
   
    //ChatService
    FailedToGetAllMessages = 300,
    GettingChatInfoIsFailed = 301,
    ChatDoesNotExist = 302,
    TheseChatParticipantsDoNotExist = 303,
    
    //RelationshipsService
    FailedToAddNewFriend = 400,
    FailedToGetFriends = 401,
    FailedToDeleteFriend = 402,
    FailedToFindFriend = 403,
    UsersAreAlreadyFriends = 404,
    UsersAreNotFriends = 405,
    FailedToAcceptRequest = 406,
    FailedToGetAllUsersWithRequests = 407,
    
}