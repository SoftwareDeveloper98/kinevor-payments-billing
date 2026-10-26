using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kinevor.Modules.PaymentsBilling.DemoApi.Data;
using Xunit;

namespace Kinevor.Modules.PaymentsBilling.Tests;

/// <summary>HTTP contract for invoices, payments, refunds and reporting.</summary>
public class BillingApiTests
{
    private const string MayaId = "11111111-1111-1111-1111-111111111111";
    private const string SofiaId = "33333333-3333-3333-3333-333333333333";

    /// <summary>Staff see every seeded invoice and the correct dashboard totals.</summary>
    [Fact]
    public async Task Staff_CanListInvoicesAndStats()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var list = StaffRequest(HttpMethod.Get, "/api/v1/billing/invoices?pageSize=20");
        var listResponse = await client.SendAsync(list);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using var listDocument = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Equal(7, listDocument.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32());

        using var stats = StaffRequest(HttpMethod.Get, "/api/v1/billing/stats");
        var statsResponse = await client.SendAsync(stats);
        Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
        using var statsDocument = JsonDocument.Parse(await statsResponse.Content.ReadAsStringAsync());
        var data = statsDocument.RootElement.GetProperty("data");
        Assert.Equal(4, data.GetProperty("openInvoiceCount").GetInt32());
        Assert.Equal(2, data.GetProperty("overdueInvoiceCount").GetInt32());
        Assert.Equal(7, data.GetProperty("totalInvoiceCount").GetInt32());
        Assert.Equal(373.20m, data.GetProperty("outstandingAmount").GetDecimal());
        Assert.Equal(136.95m, data.GetProperty("overdueAmount").GetDecimal());
        Assert.Equal("USD", data.GetProperty("currency").GetString());
    }

    /// <summary>Members cannot reach staff billing endpoints.</summary>
    [Fact]
    public async Task StaffEndpoints_ForbidMembers()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = MemberRequest(HttpMethod.Get, "/api/v1/billing/invoices", MayaId);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Member invoice lists are scoped to the linked member.</summary>
    [Fact]
    public async Task Member_SeesOnlyOwnInvoices()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = MemberRequest(HttpMethod.Get, "/api/v1/me/invoices?pageSize=20", MayaId);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("data").GetProperty("items");
        Assert.Equal(4, items.GetArrayLength());
        Assert.All(items.EnumerateArray(), item => Assert.Equal(MayaId, item.GetProperty("memberId").GetString()));

        using var other = MemberRequest(HttpMethod.Get, "/api/v1/me/invoices?pageSize=20", SofiaId);
        var otherResponse = await client.SendAsync(other);
        using var otherDocument = JsonDocument.Parse(await otherResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, otherDocument.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32());
    }

    /// <summary>A member cannot read or pay another member's invoice.</summary>
    [Fact]
    public async Task Member_CannotAccessAnotherMembersInvoice()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var invoiceId = await FindInvoiceId(client, "/api/v1/billing/invoices?search=INV-&pageSize=20", "INV-2026-000007");

        using var detail = MemberRequest(HttpMethod.Get, $"/api/v1/me/invoices/{invoiceId}", MayaId);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(detail)).StatusCode);

        using var pay = MemberRequest(HttpMethod.Post, $"/api/v1/me/invoices/{invoiceId}/pay", MayaId);
        pay.Content = JsonContent.Create(new { method = "Card", reference = "TXN-MEMBER-ATTACK" });
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(pay)).StatusCode);
    }

    /// <summary>Creating an invoice derives totals and returns a generated number.</summary>
    [Fact]
    public async Task Staff_CanCreateInvoice()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = StaffRequest(HttpMethod.Post, "/api/v1/billing/invoices");
        request.Content = JsonContent.Create(new
        {
            memberId = MayaId,
            memberName = "Maya Thompson",
            memberEmail = "maya.thompson@kinevor.demo",
            dueAt = DateTime.UtcNow.AddDays(14),
            lines = new[]
            {
                new { description = "Membership", quantity = 1, unitPrice = 49m, taxRate = 0.05m },
                new { description = "PT session", quantity = 2, unitPrice = 30m, taxRate = 0.05m },
            },
        });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");
        Assert.StartsWith("INV-2026-", data.GetProperty("number").GetString());
        Assert.Equal(114.45m, data.GetProperty("total").GetDecimal());
        Assert.Equal("USD", data.GetProperty("currency").GetString());
    }

    /// <summary>Recording a payment settles the invoice and can be retried safely.</summary>
    [Fact]
    public async Task RecordPayment_IsIdempotentByReference()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var invoiceId = await FindInvoiceId(client, "/api/v1/billing/invoices?status=Open&pageSize=20", "INV-2026-000003");

        var payload = new
        {
            memberId = MayaId,
            memberName = "Maya Thompson",
            invoiceId,
            amount = 126m,
            method = "Cash",
            reference = "TXN-TEST-IDEMPOTENT",
        };

        using var first = StaffRequest(HttpMethod.Post, "/api/v1/billing/payments");
        first.Content = JsonContent.Create(payload);
        var firstResponse = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        using var firstDocument = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        var paymentId = firstDocument.RootElement.GetProperty("data").GetProperty("id").GetString();

        using var second = StaffRequest(HttpMethod.Post, "/api/v1/billing/payments");
        second.Content = JsonContent.Create(payload);
        var secondResponse = await client.SendAsync(second);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        using var secondDocument = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
        Assert.Equal(paymentId, secondDocument.RootElement.GetProperty("data").GetProperty("id").GetString());

        using var detail = StaffRequest(HttpMethod.Get, $"/api/v1/billing/invoices/{invoiceId}");
        var detailResponse = await client.SendAsync(detail);
        using var detailDocument = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        var detailData = detailDocument.RootElement.GetProperty("data");
        Assert.Equal(126m, detailData.GetProperty("amountPaid").GetDecimal());
        Assert.Equal("Paid", detailData.GetProperty("status").GetString());

        using var list = StaffRequest(HttpMethod.Get, "/api/v1/billing/payments?search=TXN-TEST-IDEMPOTENT");
        var listResponse = await client.SendAsync(list);
        using var listDocument = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, listDocument.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32());
    }

    /// <summary>A member can pay the outstanding balance of an invoice.</summary>
    [Fact]
    public async Task Member_CanPayOwnInvoice()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var invoiceId = await FindInvoiceId(client, "/api/v1/me/invoices?pageSize=20", "INV-2026-000003", MayaId);

        using var request = MemberRequest(HttpMethod.Post, $"/api/v1/me/invoices/{invoiceId}/pay", MayaId);
        request.Content = JsonContent.Create(new { method = "Card", reference = "TXN-MEMBER-MAYA-001" });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var detail = MemberRequest(HttpMethod.Get, $"/api/v1/me/invoices/{invoiceId}", MayaId);
        var detailResponse = await client.SendAsync(detail);
        using var document = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");
        Assert.Equal("Paid", data.GetProperty("status").GetString());
        Assert.Equal(0m, data.GetProperty("outstandingAmount").GetDecimal());
    }

    /// <summary>Refunds are partial until the captured amount is exhausted.</summary>
    [Fact]
    public async Task Refund_TracksPartialAndFullStatus()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var paymentId = await FindPaymentId(client, "TXN-SEED-MAYA-001");

        using var partial = StaffRequest(HttpMethod.Post, $"/api/v1/billing/payments/{paymentId}/refund");
        partial.Content = JsonContent.Create(new { amount = 14.45m, reason = "Price adjustment" });
        var partialResponse = await client.SendAsync(partial);
        Assert.Equal(HttpStatusCode.OK, partialResponse.StatusCode);
        using var partialDocument = JsonDocument.Parse(await partialResponse.Content.ReadAsStringAsync());
        var partialData = partialDocument.RootElement.GetProperty("data");
        Assert.Equal("PartiallyRefunded", partialData.GetProperty("status").GetString());
        Assert.Equal(14.45m, partialData.GetProperty("refundedAmount").GetDecimal());

        using var over = StaffRequest(HttpMethod.Post, $"/api/v1/billing/payments/{paymentId}/refund");
        over.Content = JsonContent.Create(new { amount = 100.01m, reason = "Too much" });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(over)).StatusCode);

        using var full = StaffRequest(HttpMethod.Post, $"/api/v1/billing/payments/{paymentId}/refund");
        full.Content = JsonContent.Create(new { amount = 100m, reason = "Full refund" });
        var fullResponse = await client.SendAsync(full);
        using var fullDocument = JsonDocument.Parse(await fullResponse.Content.ReadAsStringAsync());
        var fullData = fullDocument.RootElement.GetProperty("data");
        Assert.Equal("Refunded", fullData.GetProperty("status").GetString());
        Assert.Equal(2, fullData.GetProperty("refunds").GetArrayLength());
    }

    /// <summary>Voiding a paid invoice is refused and an open invoice can be voided.</summary>
    [Fact]
    public async Task Invoice_TransitionsEnforceLifecycle()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var paidId = await FindInvoiceId(client, "/api/v1/billing/invoices?status=Paid&pageSize=20", "INV-2026-000001");
        var openId = await FindInvoiceId(client, "/api/v1/billing/invoices?status=Open&pageSize=20", "INV-2026-000003");

        using var paidVoid = StaffRequest(HttpMethod.Post, $"/api/v1/billing/invoices/{paidId}/void");
        paidVoid.Content = JsonContent.Create(new { reason = "Duplicate" });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(paidVoid)).StatusCode);

        using var voidRequest = StaffRequest(HttpMethod.Post, $"/api/v1/billing/invoices/{openId}/void");
        voidRequest.Content = JsonContent.Create(new { reason = "Raised in error" });
        var voidResponse = await client.SendAsync(voidRequest);
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        using var voidDocument = JsonDocument.Parse(await voidResponse.Content.ReadAsStringAsync());
        Assert.Equal("Void", voidDocument.RootElement.GetProperty("data").GetProperty("status").GetString());

        using var payVoided = StaffRequest(HttpMethod.Post, "/api/v1/billing/payments");
        payVoided.Content = JsonContent.Create(new
        {
            memberId = MayaId,
            invoiceId = openId,
            amount = 1m,
            method = "Cash",
            reference = "TXN-TEST-VOIDED",
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(payVoided)).StatusCode);
    }

    /// <summary>The revenue report buckets captured payments and refunds by month.</summary>
    [Fact]
    public async Task Revenue_ReturnsBucketsAndTotals()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var from = DateTime.UtcNow.AddMonths(-2).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        using var request = StaffRequest(HttpMethod.Get, $"/api/v1/billing/revenue?from={from}&to={to}");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");
        Assert.Equal("USD", data.GetProperty("currency").GetString());
        Assert.True(data.GetProperty("buckets").GetArrayLength() >= 2);
        Assert.True(data.GetProperty("grossCollected").GetDecimal() >= 30m);
        Assert.True(data.GetProperty("refunded").GetDecimal() >= 10m);
        Assert.Equal(
            data.GetProperty("grossCollected").GetDecimal() - data.GetProperty("refunded").GetDecimal(),
            data.GetProperty("netRevenue").GetDecimal());
    }

    /// <summary>Creating an invoice without lines returns a validation error.</summary>
    [Fact]
    public async Task CreateInvoice_WithoutLines_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = StaffRequest(HttpMethod.Post, "/api/v1/billing/invoices");
        request.Content = JsonContent.Create(new
        {
            memberId = MayaId,
            memberName = "Maya Thompson",
            lines = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
    }

    /// <summary>Health endpoint stays anonymous and healthy.</summary>
    [Fact]
    public async Task Health_IsAnonymous()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpRequestMessage StaffRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Demo-Role", "Staff");
        return request;
    }

    private static HttpRequestMessage MemberRequest(HttpMethod method, string url, string memberId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Demo-Role", "Member");
        request.Headers.Add("X-Demo-Member", memberId);
        return request;
    }

    private static async Task<string> FindInvoiceId(
        HttpClient client,
        string listUrl,
        string number,
        string? memberId = null)
    {
        var request = memberId is null
            ? StaffRequest(HttpMethod.Get, listUrl)
            : MemberRequest(HttpMethod.Get, listUrl, memberId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var item in document.RootElement.GetProperty("data").GetProperty("items").EnumerateArray())
        {
            if (item.GetProperty("number").GetString() == number)
                return item.GetProperty("id").GetString()!;
        }

        throw new InvalidOperationException($"Invoice {number} was not found.");
    }

    private static async Task<string> FindPaymentId(HttpClient client, string reference)
    {
        using var request = StaffRequest(HttpMethod.Get, $"/api/v1/billing/payments?search={reference}");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = document.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().Single();
        return item.GetProperty("id").GetString()!;
    }
}
