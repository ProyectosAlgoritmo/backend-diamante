namespace BackendDiamante.Models.Enums;

public static class NotificationType
{
    // MVP — alertas operativas
    public const string EmployeeAbsent      = "EMPLOYEE_ABSENT";
    public const string EmployeeLate        = "EMPLOYEE_LATE";
    public const string RoutineNotStarted   = "ROUTINE_NOT_STARTED";
    public const string RoutineNotCompleted = "ROUTINE_NOT_COMPLETED";
    public const string ActivityNotCompleted = "ACTIVITY_NOT_COMPLETED";

    // Futuros
    // public const string CostCenterWithoutCoverage = "COST_CENTER_WITHOUT_COVERAGE";
    // public const string ShiftWithoutAssignedStaff = "SHIFT_WITHOUT_ASSIGNED_STAFF";
    // public const string ExpiredCertificate        = "EXPIRED_CERTIFICATE";
    // public const string CertificateExpiringSoon   = "CERTIFICATE_EXPIRING_SOON";
    // public const string PendingInspection         = "PENDING_INSPECTION";
}
