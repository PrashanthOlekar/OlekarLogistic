namespace ProCargo.Infrastructure.Data;

/// <summary>Every stored procedure the API calls, defined in database/procedures/*.sql.</summary>
internal static class StoredProcedures
{
    // 1. Reference data and settings
    public const string ReferenceDataGet = "dbo.usp_ReferenceData_Get";
    public const string CityGetById = "dbo.usp_City_GetById";
    public const string VehicleTypeGetById = "dbo.usp_VehicleType_GetById";
    public const string VehicleTypeGetSmallestForWeight = "dbo.usp_VehicleType_GetSmallestForWeight";
    public const string GoodsCategoryGetById = "dbo.usp_GoodsCategory_GetById";
    public const string SettingGetAll = "dbo.usp_Setting_GetAll";

    // 2. Accounts
    public const string UserGetById = "dbo.usp_User_GetById";
    public const string UserGetByMobile = "dbo.usp_User_GetByMobile";
    public const string UserRecordLogin = "dbo.usp_User_RecordLogin";
    public const string UserGetProfile = "dbo.usp_User_GetProfile";
    public const string UserEnsureAdmin = "dbo.usp_User_EnsureAdmin";
    public const string UserGetPaged = "dbo.usp_User_GetPaged";
    public const string UserSetStatus = "dbo.usp_User_SetStatus";
    public const string OtpCodeCountRecent = "dbo.usp_OtpCode_CountRecent";
    public const string OtpCodeCreate = "dbo.usp_OtpCode_Create";
    public const string OtpCodeGetLatestActive = "dbo.usp_OtpCode_GetLatestActive";
    public const string OtpCodeRecordFailedAttempt = "dbo.usp_OtpCode_RecordFailedAttempt";
    public const string OtpCodeMarkUsed = "dbo.usp_OtpCode_MarkUsed";
    public const string RefreshTokenCreate = "dbo.usp_RefreshToken_Create";
    public const string RefreshTokenGetByHash = "dbo.usp_RefreshToken_GetByHash";
    public const string RefreshTokenRotate = "dbo.usp_RefreshToken_Rotate";
    public const string RefreshTokenRevoke = "dbo.usp_RefreshToken_Revoke";
    public const string RefreshTokenRevokeAllForUser = "dbo.usp_RefreshToken_RevokeAllForUser";
    public const string RegistrationCreateCustomer = "dbo.usp_Registration_CreateCustomer";
    public const string RegistrationCreateOwner = "dbo.usp_Registration_CreateOwner";
    public const string RegistrationCreateDriver = "dbo.usp_Registration_CreateDriver";
    public const string DriverLicenceExists = "dbo.usp_Driver_LicenceExists";
    public const string AuditLogCreate = "dbo.usp_AuditLog_Create";
    public const string AuditLogGetPaged = "dbo.usp_AuditLog_GetPaged";

    // 3. Owners, drivers and vehicles
    public const string CustomerGetByUserId = "dbo.usp_Customer_GetByUserId";
    public const string OwnerGetById = "dbo.usp_Owner_GetById";
    public const string OwnerGetByUserId = "dbo.usp_Owner_GetByUserId";
    public const string OwnerGetByMobile = "dbo.usp_Owner_GetByMobile";
    public const string OwnerSetVerification = "dbo.usp_Owner_SetVerification";
    public const string DashboardGetOwner = "dbo.usp_Dashboard_GetOwner";
    public const string DriverGetById = "dbo.usp_Driver_GetById";
    public const string DriverGetByUserId = "dbo.usp_Driver_GetByUserId";
    public const string DriverHasActiveTrip = "dbo.usp_Driver_HasActiveTrip";
    public const string DriverGetPaged = "dbo.usp_Driver_GetPaged";
    public const string DriverSetVerification = "dbo.usp_Driver_SetVerification";
    public const string VehicleGetById = "dbo.usp_Vehicle_GetById";
    public const string VehicleHasActiveTrip = "dbo.usp_Vehicle_HasActiveTrip";
    public const string VehicleGetPaged = "dbo.usp_Vehicle_GetPaged";
    public const string VehicleCreate = "dbo.usp_Vehicle_Create";
    public const string VehiclePatch = "dbo.usp_Vehicle_Patch";
    public const string VehicleSetVerification = "dbo.usp_Vehicle_SetVerification";
    public const string ApprovalGetPending = "dbo.usp_Approval_GetPending";

    // 4. Bookings, quotes, payments and loads
    public const string BookingCreate = "dbo.usp_Booking_Create";
    public const string BookingGetById = "dbo.usp_Booking_GetById";
    public const string BookingGetPaged = "dbo.usp_Booking_GetPaged";
    public const string BookingGetDetail = "dbo.usp_Booking_GetDetail";
    public const string QuoteGetLatestOpen = "dbo.usp_Quote_GetLatestOpen";
    public const string BookingRequote = "dbo.usp_Booking_Requote";
    public const string BookingPay = "dbo.usp_Booking_Pay";
    public const string BookingCancel = "dbo.usp_Booking_Cancel";
    public const string BookingGetAssignableVehicles = "dbo.usp_Booking_GetAssignableVehicles";
    public const string LoadGetAvailable = "dbo.usp_Load_GetAvailable";
    public const string LoadOfferDecline = "dbo.usp_LoadOffer_Decline";
    public const string PaymentGetPaged = "dbo.usp_Payment_GetPaged";

    // 5. Trips
    public const string TripAssign = "dbo.usp_Trip_Assign";
    public const string TripGetById = "dbo.usp_Trip_GetById";
    public const string TripGetPaged = "dbo.usp_Trip_GetPaged";
    public const string TripGetDetail = "dbo.usp_Trip_GetDetail";
    public const string TripChangeStatus = "dbo.usp_Trip_ChangeStatus";
    public const string TripHasPod = "dbo.usp_Trip_HasPod";
    public const string TripCompleteDelivery = "dbo.usp_Trip_CompleteDelivery";
    public const string TripAddPhoto = "dbo.usp_Trip_AddPhoto";
    public const string TripGetForPodApproval = "dbo.usp_Trip_GetForPodApproval";
    public const string InvoiceNextSequence = "dbo.usp_Invoice_NextSequence";
    public const string TripApprovePod = "dbo.usp_Trip_ApprovePod";

    // 6. Documents, payouts and the admin dashboard
    public const string DocumentCreate = "dbo.usp_Document_Create";
    public const string DocumentGetById = "dbo.usp_Document_GetById";
    public const string DocumentGetPaged = "dbo.usp_Document_GetPaged";
    public const string DocumentReview = "dbo.usp_Document_Review";
    public const string SettlementGetById = "dbo.usp_Settlement_GetById";
    public const string SettlementGetPaged = "dbo.usp_Settlement_GetPaged";
    public const string SettlementRecordPayout = "dbo.usp_Settlement_RecordPayout";
    public const string DashboardGetAdmin = "dbo.usp_Dashboard_GetAdmin";
}
