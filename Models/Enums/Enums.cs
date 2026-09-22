namespace CampusConnect.Models.Enums;

public enum UserRole { Student, Faculty, ClubCoordinator, EventOrganizer, Hod, Admin }
public enum UserStatus { Active, Suspended, PendingVerification }
public enum ProficiencyLevel { Beginner, Intermediate, Advanced }
public enum OpportunityCategory { Internship, Job, Hackathon, Workshop, Seminar, CulturalEvent }
public enum WorkMode { Remote, Onsite, Hybrid }
public enum ApprovalStatus { PendingReview, Approved, Rejected }
public enum ApplicationStatus { Registered, Applied, UnderReview, Shortlisted, Selected, Rejected }
public enum GrievanceCategory { Infrastructure, Academic, HostelMess, HarassmentRagging, Canteen, Administration }
public enum GrievancePriority { Low, Medium, High, Critical }
public enum GrievanceStatus { Submitted, Acknowledged, InProgress, Resolved, Escalated }
public enum ConnectionStatus { Pending, Accepted, Declined }
