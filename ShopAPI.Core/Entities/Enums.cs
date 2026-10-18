namespace ShopAPI.Core.Entities;

public enum Role
{
    User = 0,
    Admin = 1
}

public enum DiscountType
{
    Percentage = 0,
    Fixed = 1
}

public enum RefundStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}