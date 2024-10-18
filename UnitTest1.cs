using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Nikhil_PART2_PROG6212_CMCSF.Data;
using Nikhil_PART2_PROG6212_CMCSF.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class ClaimsControllerTests
{
    private ClaimsController _controller;
    private ApplicationDbContext _dbContext;

    public ClaimsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _controller = new ClaimsController(_dbContext);
    }

    /// <summary>
    /// Reset database state before each test to prevent interference.
    /// </summary>
    private void ResetDatabase()
    {
        _dbContext.Database.EnsureDeleted();  // Clear the database
        _dbContext.Database.EnsureCreated();  // Recreate the database
    }

    private Claim CreateValidClaim(int id)
    {
        return new Claim
        {
            ClaimId = id,
            Status = "Pending",
            DocumentPath = $"/uploads/test{id}.pdf",
            LecturerName = "John Doe",
            Notes = "Test notes"
        };
    }

    [Fact]
    public async Task SubmitClaim_ValidClaimWithFile_ReturnsRedirectToAction()
    {
        // Arrange
        ResetDatabase(); // Reset the database
        var claim = CreateValidClaim(1);
        var fileMock = new Mock<IFormFile>();

        var content = "Dummy file content";
        var fileName = "test.pdf";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);

        // Act
        var result = await _controller.SubmitClaim(claim, fileMock.Object);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ClaimSubmitted", redirectResult.ActionName);
        Assert.Single(_dbContext.Claims);
    }

    [Fact]
    public async Task ViewPendingClaims_ReturnsViewWithPendingClaims()
    {
        // Arrange
        ResetDatabase(); // Reset the database
        _dbContext.Claims.Add(CreateValidClaim(1));

        var approvedClaim = CreateValidClaim(2);
        approvedClaim.Status = "Approved";
        _dbContext.Claims.Add(approvedClaim);

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.ViewPendingClaims();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Claim>>(viewResult.ViewData.Model);
        Assert.Single(model);
        Assert.Equal("Pending", model.First().Status);
    }


    [Fact]
    public async Task ApproveClaim_ValidId_UpdatesClaimStatus()
    {
        // Arrange
        ResetDatabase(); // Reset the database
        var claim = CreateValidClaim(1);
        _dbContext.Claims.Add(claim);
        await _dbContext.SaveChangesAsync();

        // Act
        await _controller.ApproveClaim(claim.ClaimId);

        // Assert
        var updatedClaim = await _dbContext.Claims.FindAsync(claim.ClaimId);
        Assert.Equal("Approved", updatedClaim.Status);
    }

    [Fact]
    public async Task RejectClaim_InvalidId_ReturnsErrorView()
    {
        // Arrange
        ResetDatabase(); // Reset the database

        // Act
        var result = await _controller.RejectClaim(99); // Non-existent claim ID

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewPendingClaims", redirectResult.ActionName);
    }

    [Fact]
    public async Task DeleteClaim_ValidId_DeletesClaim()
    {
        // Arrange
        ResetDatabase(); // Reset the database
        var claim = CreateValidClaim(1);
        _dbContext.Claims.Add(claim);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteClaim(claim.ClaimId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("TrackClaims", redirectResult.ActionName);
        Assert.Empty(_dbContext.Claims);
    }
}
