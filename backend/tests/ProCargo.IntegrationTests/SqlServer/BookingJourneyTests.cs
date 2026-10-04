using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProCargo.IntegrationTests.Infrastructure;

namespace ProCargo.IntegrationTests.SqlServer;

/// <summary>
/// The whole business on a real database, through the real API:
/// sign-up → KYC approval → booking → payment → owner takes the load → driver runs the trip
/// → POD approval and invoice → owner payout. Every step goes through the stored procedures.
/// </summary>
public sealed class BookingJourneyTests(SqlServerApiFactory factory) : IClassFixture<SqlServerApiFactory>
{
    private readonly ProCargoClient _api = new(factory);

    [SqlServerFact]
    public async Task Booking_to_owner_payout()
    {
        // ---------------------------------------------------------------- people
        ProCargoClient.SignedIn admin = await _api.SignInAsync(ProCargoClient.AdminMobile);
        ProCargoClient.SignedIn owner = await _api.RegisterOwnerAsync();
        ProCargoClient.SignedIn driver = await _api.RegisterDriverAsync(owner.Mobile);
        ProCargoClient.SignedIn customer = await _api.RegisterCustomerAsync();

        long ownerId = owner.DetailId("ownerId");
        long driverId = driver.DetailId("driverId");
        Assert.Equal("PendingKyc", owner.Profile.Text("status"));
        Assert.Equal("Manjunath Patil", driver.Profile.GetProperty("detail").Text("ownerName"));

        // ---------------------------------------------------------------- reference data
        JsonElement lists = await _api.Anonymous().GetJsonAsync("/api/v1/reference-data");
        long bengaluru = lists.GetProperty("cities").EnumerateArray().Single(city => city.Text("name") == "Bengaluru").Id();
        long hubballi = lists.GetProperty("cities").EnumerateArray().Single(city => city.Text("name") == "Hubballi").Id();
        long fourteenFeet = lists.GetProperty("vehicleTypes").EnumerateArray().Single(type => type.Text("code") == "14FT").Id();
        long goods = lists.GetProperty("goods").EnumerateArray().First().Id();

        // ---------------------------------------------------------------- fleet and KYC
        string registration = JourneyHelpers.NewRegistrationNumber();
        HttpResponseMessage added = await owner.Http.PostAsJsonAsync("/api/v1/vehicles", new
        {
            registrationNumber = registration[..2] + " " + registration[2..],
            vehicleTypeId = fourteenFeet,
            capacityKg = 4000,
            makeModel = "Tata 1412",
            manufactureYear = 2022,
            homeCityId = bengaluru,
        });
        long vehicleId = (await added.ShouldBeAsync<JsonElement>(HttpStatusCode.Created)).Id();

        // Same registration again: conflict from the unique index.
        await owner.Http.PostAsJsonAsync("/api/v1/vehicles", new { registrationNumber = registration, vehicleTypeId = fourteenFeet, capacityKg = 4000 })
            .ExpectAsync(HttpStatusCode.Conflict);

        HttpResponseMessage rc = await owner.Http.UploadAsync("/api/v1/documents", "rc.jpg", "image/jpeg",
            ("entityType", "Vehicle"), ("entityId", vehicleId.ToString()), ("docType", "RC"), ("documentNumber", "RC-1"), ("expiryDate", "2030-01-31"));
        long rcDocumentId = (await rc.ShouldBeAsync<JsonElement>(HttpStatusCode.Created)).Id();

        await owner.Http.UploadAsync("/api/v1/documents", "pan.pdf", "application/pdf", ("entityType", "Owner"), ("docType", "PAN"))
            .ExpectAsync(HttpStatusCode.Created);

        // A customer can't attach documents to someone else's vehicle.
        await customer.Http.UploadAsync("/api/v1/documents", "rc.jpg", "image/jpeg", ("entityType", "Vehicle"), ("entityId", vehicleId.ToString()), ("docType", "RC"))
            .ExpectAsync(HttpStatusCode.Forbidden);

        JsonElement vehicles = await owner.Http.GetJsonAsync("/api/v1/vehicles");
        JsonElement myVehicle = vehicles.Items().Single(vehicle => vehicle.Id() == vehicleId);
        Assert.Equal(registration, myVehicle.Text("registrationNumber"));
        Assert.Equal("Pending", myVehicle.Text("verificationStatus"));
        Assert.Contains(myVehicle.GetProperty("documents").EnumerateArray(), document => document.Text("docType") == "RC");

        JsonElement approvals = await admin.Http.GetJsonAsync("/api/v1/approvals");
        Assert.Contains(approvals.GetProperty("owners").EnumerateArray(), pending => pending.Id() == ownerId);
        Assert.Contains(approvals.GetProperty("drivers").EnumerateArray(), pending => pending.Id() == driverId);
        Assert.Contains(approvals.GetProperty("vehicles").EnumerateArray(), pending => pending.Id() == vehicleId);

        await admin.Http.PutJsonAsync($"/api/v1/owners/{ownerId}/verification", new { approve = false }).ExpectAsync(HttpStatusCode.BadRequest);
        await admin.Http.PutJsonAsync($"/api/v1/owners/{ownerId}/verification", new { approve = true }).ExpectAsync(HttpStatusCode.NoContent);
        await admin.Http.PutJsonAsync($"/api/v1/drivers/{driverId}/verification", new { approve = true }).ExpectAsync(HttpStatusCode.NoContent);
        await admin.Http.PutJsonAsync($"/api/v1/vehicles/{vehicleId}/verification", new { approve = true }).ExpectAsync(HttpStatusCode.NoContent);
        await admin.Http.PutJsonAsync($"/api/v1/documents/{rcDocumentId}/review", new { approve = true }).ExpectAsync(HttpStatusCode.NoContent);
        await admin.Http.PutJsonAsync("/api/v1/owners/999999999/verification", new { approve = true }).ExpectAsync(HttpStatusCode.NotFound);

        JsonElement ownerProfile = await owner.Http.GetJsonAsync("/api/v1/auth/me");
        Assert.Equal("Active", ownerProfile.Text("status"));
        Assert.Equal("Approved", ownerProfile.GetProperty("detail").Text("kycStatus"));

        // The owner names the driver as the truck's regular driver.
        await owner.Http.PatchAsJsonAsync($"/api/v1/vehicles/{vehicleId}", new { currentDriverId = driverId }).ExpectAsync(HttpStatusCode.NoContent);

        // ---------------------------------------------------------------- booking and payment
        JsonElement estimate = await (await _api.Anonymous().PostAsJsonAsync("/api/v1/price-estimates",
            new { pickupCityId = bengaluru, dropCityId = hubballi, vehicleTypeId = fourteenFeet, weightKg = 3000 }))
            .ShouldBeAsync<JsonElement>(HttpStatusCode.OK);
        Assert.True(estimate.GetProperty("totalAmount").GetDecimal() > 0);
        Assert.False(estimate.GetProperty("overCapacity").GetBoolean());

        HttpResponseMessage created = await customer.Http.PostAsJsonAsync("/api/v1/bookings", new
        {
            pickupCityId = bengaluru,
            pickupAddress = "Plot 12, Peenya Industrial Area",
            pickupContactName = "Ravi",
            pickupContactPhone = "9845012345",
            dropCityId = hubballi,
            dropAddress = "Gokul Road",
            goodsCategoryId = goods,
            goodsDescription = "40 cartons of biscuits",
            weightKg = 3000,
            goodsValue = 85000,
            vehicleTypeId = fourteenFeet,
            pickupDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)).ToString("yyyy-MM-dd"),
            pickupSlot = "Morning",
        });
        JsonElement newBooking = await created.ShouldBeAsync<JsonElement>(HttpStatusCode.Created);
        long bookingId = newBooking.Id();
        Assert.StartsWith("PC-", newBooking.Text("bookingNumber"));
        Assert.EndsWith($"/api/v1/bookings/{bookingId}", created.Headers.Location!.ToString());

        JsonElement quoted = await customer.Http.GetJsonAsync($"/api/v1/bookings/{bookingId}");
        Assert.Equal("Quoted", quoted.Text("status"));
        Assert.Equal(estimate.GetProperty("totalAmount").GetDecimal(), quoted.GetProperty("quote").GetProperty("totalAmount").GetDecimal());

        await (await owner.Http.GetAsync($"/api/v1/bookings/{bookingId}")).ShouldBeProblemAsync(HttpStatusCode.Forbidden);

        await customer.Http.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/quotes", new { }).ExpectAsync(HttpStatusCode.Created);
        await customer.Http.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/payments", new { method = "Cash" }).ExpectAsync(HttpStatusCode.BadRequest);
        await customer.Http.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/payments", new { method = "UPI" }).ExpectAsync(HttpStatusCode.Created);
        await customer.Http.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/payments", new { method = "UPI" }).ExpectAsync(HttpStatusCode.Conflict);

        // ---------------------------------------------------------------- the owner takes the load
        JsonElement loads = await owner.Http.GetJsonAsync("/api/v1/loads");
        Assert.True(loads.GetProperty("kycApproved").GetBoolean());
        JsonElement load = loads.GetProperty("loads").EnumerateArray().Single(item => item.Id() == bookingId);
        Assert.Contains(load.GetProperty("vehicles").EnumerateArray(), vehicle => vehicle.Id() == vehicleId);

        await customer.Http.PostAsJsonAsync("/api/v1/trips", new { bookingId, vehicleId, driverId }).ExpectAsync(HttpStatusCode.Forbidden);

        HttpResponseMessage assigned = await owner.Http.PostAsJsonAsync("/api/v1/trips", new { bookingId, vehicleId, driverId });
        JsonElement trip = await assigned.ShouldBeAsync<JsonElement>(HttpStatusCode.Created);
        long tripId = trip.Id();
        string tripNumber = trip.Text("tripNumber");

        await owner.Http.PostAsJsonAsync("/api/v1/trips", new { bookingId, vehicleId, driverId }).ExpectAsync(HttpStatusCode.Conflict);

        JsonElement assignedBooking = await customer.Http.GetJsonAsync($"/api/v1/bookings/{bookingId}");
        Assert.Equal("Assigned", assignedBooking.Text("status"));
        string pickupCode = assignedBooking.GetProperty("trip").Text("pickupOtp");
        string deliveryCode = assignedBooking.GetProperty("trip").Text("deliveryOtp");
        Assert.Matches("^[0-9]{4}$", pickupCode);

        JsonElement adminView = await admin.Http.GetJsonAsync($"/api/v1/bookings/{bookingId}");
        Assert.Equal(JsonValueKind.Null, adminView.GetProperty("trip").GetProperty("pickupOtp").ValueKind);

        // ---------------------------------------------------------------- the driver runs the trip
        JsonElement myTrips = await driver.Http.GetJsonAsync("/api/v1/trips");
        Assert.Contains(myTrips.Items(), item => item.Id() == tripId);
        Assert.Equal(JsonValueKind.Null, myTrips.Items().First(item => item.Id() == tripId).GetProperty("ownerPayout").ValueKind);

        JsonElement detail = await driver.Http.GetJsonAsync($"/api/v1/trips/{tripId}");
        Assert.Equal("Gokul Road", detail.GetProperty("booking").GetProperty("drop").Text("address"));

        string events = $"/api/v1/trips/{tripId}/events";
        string handovers = $"/api/v1/trips/{tripId}/handovers";
        string photos = $"/api/v1/trips/{tripId}/photos";

        await driver.Http.PostAsJsonAsync(events, new { action = "StartTrip" }).ExpectAsync(HttpStatusCode.Conflict);
        await driver.Http.PostAsJsonAsync(events, new { action = "EnRoute", latitude = 13.02m, longitude = 77.55m }).ExpectAsync(HttpStatusCode.NoContent);
        await driver.Http.PostAsJsonAsync(events, new { action = "ReachedPickup" }).ExpectAsync(HttpStatusCode.NoContent);
        await driver.Http.UploadAsync(photos, "goods.jpg", "image/jpeg", ("kind", "PickupPhoto")).ExpectAsync(HttpStatusCode.Created);
        await driver.Http.PostAsJsonAsync(handovers, new { kind = "Pickup", code = pickupCode == "0000" ? "0001" : "0000" }).ExpectAsync(HttpStatusCode.BadRequest);
        await driver.Http.PostAsJsonAsync(handovers, new { kind = "Pickup", code = pickupCode }).ExpectAsync(HttpStatusCode.NoContent);
        await driver.Http.PostAsJsonAsync(events, new { action = "StartTrip" }).ExpectAsync(HttpStatusCode.NoContent);

        await customer.Http.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/cancellation", new { reason = "Changed my mind" }).ExpectAsync(HttpStatusCode.BadRequest);

        await driver.Http.PostAsJsonAsync(events, new { action = "ReachedDestination" }).ExpectAsync(HttpStatusCode.NoContent);
        await driver.Http.PostAsJsonAsync(handovers, new { kind = "Delivery", code = deliveryCode }).ExpectAsync(HttpStatusCode.BadRequest);
        await driver.Http.UploadAsync(photos, "pod.jpg", "image/jpeg", ("kind", "POD"), ("latitude", "15.36"), ("longitude", "75.12")).ExpectAsync(HttpStatusCode.Created);
        await driver.Http.PostAsJsonAsync(handovers, new { kind = "Delivery", code = deliveryCode }).ExpectAsync(HttpStatusCode.NoContent);

        // ---------------------------------------------------------------- POD approval, invoice and payout
        JsonElement delivered = await admin.Http.GetJsonAsync($"/api/v1/trips?status=Delivered&search={tripNumber}");
        JsonElement deliveredTrip = delivered.Items().Single(item => item.Text("tripNumber") == tripNumber);
        Assert.Equal(JsonValueKind.Number, deliveredTrip.GetProperty("pod").ValueKind);

        await admin.Http.PutAsync($"/api/v1/trips/{tripId}/pod-approval", null).ExpectAsync(HttpStatusCode.NoContent);
        await admin.Http.PutAsync($"/api/v1/trips/{tripId}/pod-approval", null).ExpectAsync(HttpStatusCode.BadRequest);

        JsonElement payouts = await admin.Http.GetJsonAsync($"/api/v1/settlements?status=Approved&search={tripNumber}");
        JsonElement payout = payouts.Items().Single(item => item.Text("trip") == tripNumber);
        Assert.Equal(payout.GetProperty("grossAmount").GetDecimal() - payout.GetProperty("commissionAmount").GetDecimal(), payout.GetProperty("netAmount").GetDecimal());

        await admin.Http.PutJsonAsync($"/api/v1/settlements/{payout.Id()}/payout", new { utr = "" }).ExpectAsync(HttpStatusCode.BadRequest);
        await admin.Http.PutJsonAsync($"/api/v1/settlements/{payout.Id()}/payout", new { utr = "HDFCN52026100412345" }).ExpectAsync(HttpStatusCode.NoContent);
        await admin.Http.PutJsonAsync($"/api/v1/settlements/{payout.Id()}/payout", new { utr = "HDFCN52026100412345" }).ExpectAsync(HttpStatusCode.BadRequest);

        JsonElement completed = await customer.Http.GetJsonAsync($"/api/v1/bookings/{bookingId}");
        Assert.Equal("Completed", completed.Text("status"));
        Assert.StartsWith("PC/", completed.GetProperty("invoice").Text("invoiceNumber"));
        Assert.Equal(completed.GetProperty("quote").GetProperty("totalAmount").GetDecimal(), completed.GetProperty("invoice").GetProperty("totalAmount").GetDecimal());
        Assert.True(completed.GetProperty("invoice").GetProperty("cgst").GetDecimal() > 0);   // both cities in Karnataka

        JsonElement ownerPayouts = await owner.Http.GetJsonAsync("/api/v1/settlements");
        Assert.Equal("Released", ownerPayouts.Items().Single().Text("status"));

        JsonElement ownerDashboard = await owner.Http.GetJsonAsync("/api/v1/dashboards/owner");
        Assert.Equal(payout.GetProperty("netAmount").GetDecimal(), ownerDashboard.GetProperty("earnedThisMonth").GetDecimal());
        Assert.Equal("9012", ownerDashboard.GetProperty("bank").Text("accountLast4"));

        JsonElement adminDashboard = await admin.Http.GetJsonAsync("/api/v1/dashboards/admin");
        Assert.Equal(7, adminDashboard.GetProperty("last7").GetArrayLength());
        Assert.True(adminDashboard.GetProperty("last7").EnumerateArray().Last().GetProperty("count").GetInt32() >= 1);

        JsonElement audit = await admin.Http.GetJsonAsync("/api/v1/audit-logs?search=Trip.PodApproved&pageSize=5");
        Assert.True(audit.GetProperty("totalRecords").GetInt64() >= 1);

        JsonElement payments = await admin.Http.GetJsonAsync($"/api/v1/payments?search={newBooking.Text("bookingNumber")}");
        Assert.Equal("Captured", payments.Items().Single(item => item.Text("bookingNumber") == newBooking.Text("bookingNumber")).Text("status"));

        JsonElement onePerPage = await admin.Http.GetJsonAsync("/api/v1/bookings?pageNumber=1&pageSize=1");
        Assert.Single(onePerPage.Items());
        Assert.True(onePerPage.GetProperty("totalPages").GetInt32() >= 1);
    }

    [SqlServerFact]
    public async Task Customer_cancels_a_paid_booking_and_is_refunded()
    {
        ProCargoClient.SignedIn customer = await _api.RegisterCustomerAsync();
        JsonElement lists = await _api.Anonymous().GetJsonAsync("/api/v1/reference-data");
        long mysuru = lists.GetProperty("cities").EnumerateArray().Single(city => city.Text("name") == "Mysuru").Id();
        long ace = lists.GetProperty("vehicleTypes").EnumerateArray().Single(type => type.Text("code") == "ACE").Id();

        HttpResponseMessage tooHeavy = await customer.Http.PostAsJsonAsync("/api/v1/bookings", new
        {
            pickupCityId = mysuru, pickupAddress = "Hebbal", dropCityId = mysuru, dropAddress = "Vijayanagar",
            goodsCategoryId = 1, goodsDescription = "Sofa set", weightKg = 2000, vehicleTypeId = ace,
            pickupDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd"),
        });
        JsonElement problem = await tooHeavy.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal("Tata Ace carries up to 750 kg. Choose a larger vehicle.", problem.Text("detail"));

        HttpResponseMessage created = await customer.Http.PostAsJsonAsync("/api/v1/bookings", new
        {
            pickupCityId = mysuru, pickupAddress = "Hebbal", dropCityId = mysuru, dropAddress = "Vijayanagar",
            goodsCategoryId = 1, goodsDescription = "Sofa set", weightKg = 300, vehicleTypeId = ace,
            pickupDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd"),
        });
        long bookingId = (await created.ShouldBeAsync<JsonElement>(HttpStatusCode.Created)).Id();

        await customer.Http.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/payments", new { method = "NetBanking" }).ExpectAsync(HttpStatusCode.Created);
        await customer.Http.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/cancellation", new { reason = "Moving postponed" }).ExpectAsync(HttpStatusCode.NoContent);

        JsonElement cancelled = await customer.Http.GetJsonAsync($"/api/v1/bookings/{bookingId}");
        Assert.Equal("Cancelled", cancelled.Text("status"));
        Assert.Equal("Moving postponed", cancelled.Text("cancelledReason"));
        Assert.Equal("Refunded", cancelled.GetProperty("payment").Text("status"));

        JsonElement mine = await customer.Http.GetJsonAsync("/api/v1/bookings?status=Cancelled");
        Assert.Contains(mine.Items(), booking => booking.Id() == bookingId);
    }

    [SqlServerFact]
    public async Task Accounts_refresh_sign_out_and_blocking()
    {
        ProCargoClient.SignedIn admin = await _api.SignInAsync(ProCargoClient.AdminMobile);
        ProCargoClient.SignedIn customer = await _api.RegisterCustomerAsync();

        // A number can only be registered once.
        await _api.Anonymous().PostAsJsonAsync("/api/v1/auth/otp", new { mobile = customer.Mobile, purpose = "Signup" }).ExpectAsync(HttpStatusCode.Conflict);

        // Refresh tokens work once.
        HttpResponseMessage refreshed = await _api.Anonymous().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = customer.RefreshToken });
        JsonElement next = await refreshed.ShouldBeAsync<JsonElement>(HttpStatusCode.OK);
        await _api.Anonymous().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = customer.RefreshToken }).ExpectAsync(HttpStatusCode.Unauthorized);

        // Re-using the old token revoked the whole family, including the newest one.
        await _api.Anonymous().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = next.Text("refreshToken") }).ExpectAsync(HttpStatusCode.Unauthorized);

        // Blocking stops the account at once; unblocking lets it back in.
        JsonElement found = await admin.Http.GetJsonAsync($"/api/v1/users?search={customer.Mobile}");
        long userId = found.Items().Single().Id();

        await admin.Http.PatchAsJsonAsync($"/api/v1/users/{userId}", new { status = "Blocked" }).ExpectAsync(HttpStatusCode.NoContent);
        await customer.Http.GetAsync("/api/v1/auth/me").ExpectAsync(HttpStatusCode.Forbidden);

        await admin.Http.PatchAsJsonAsync($"/api/v1/users/{userId}", new { status = "Active" }).ExpectAsync(HttpStatusCode.NoContent);
        await customer.Http.GetAsync("/api/v1/auth/me").ExpectAsync(HttpStatusCode.OK);

        await customer.Http.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = customer.RefreshToken }).ExpectAsync(HttpStatusCode.NoContent);
        await admin.Http.PatchAsJsonAsync("/api/v1/users/999999999", new { status = "Blocked" }).ExpectAsync(HttpStatusCode.NotFound);
    }
}
