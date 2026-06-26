namespace HelpDesk.Domain.Enums;

public enum UserRole { Admin = 1, Manager = 2, Agent = 3, Customer = 4 }
public enum TicketStatus { New = 1, Open = 2, InProgress = 3, Resolved = 4, Closed = 5 }
public enum TicketPriority { Low = 1, Medium = 2, High = 3, Urgent = 4 }
public enum TicketCategory { Technical = 1, Billing = 2, Account = 3, General = 4, FeatureRequest = 5 }
public enum ActivityType { Created = 1, Assigned = 2, StatusChanged = 3, Commented = 4, AttachmentAdded = 5, Closed = 6, Reopened = 7 }
