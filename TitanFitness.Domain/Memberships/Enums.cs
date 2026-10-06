namespace TitanFitness.Domain.Memberships;

public enum AccessScope
{
    HomeBranchOnly = 1,
    AllBranches = 2
}

public enum MembershipStatus
{
    Pending = 1,
    Active = 2,
    Frozen = 3,
    Expired = 4,
    Cancelled = 5
}

public enum FreezeReason
{
    ExtendedTravel = 1,
    Injury = 2,
    Other = 3,
    Medical = 4,
    Financial = 5
}
