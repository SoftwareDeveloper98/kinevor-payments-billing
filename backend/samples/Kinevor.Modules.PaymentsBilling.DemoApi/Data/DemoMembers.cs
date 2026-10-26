namespace Kinevor.Modules.PaymentsBilling.DemoApi.Data;

/// <summary>Deterministic identifiers used by the demo fixtures and screenshot flows.</summary>
public static class DemoMembers
{
    /// <summary>Maya Thompson, the primary demo member.</summary>
    public static readonly Guid Maya = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Daniel Okafor, a second member with overdue balances.</summary>
    public static readonly Guid Daniel = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>Sofia Rossi, a third member with a refunded payment.</summary>
    public static readonly Guid Sofia = Guid.Parse("33333333-3333-3333-3333-333333333333");
}
