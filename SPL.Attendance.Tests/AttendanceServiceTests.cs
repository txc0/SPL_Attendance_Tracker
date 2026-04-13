using FluentAssertions;
using Moq;
using SPL.Attendance.Business.Services;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;
using Xunit;

namespace SPL.Attendance.Tests
{
    /// <summary>
    /// Fingerprint-style attendance tests.
    /// Employees can only sign in; multiple sign-ins require supervisor approval.
    /// </summary>
    public class AttendanceServiceTests
    {
        private readonly Mock<IAttendanceRepository> _repoMock;
        private readonly Mock<IShowCauseRepository> _showCauseMock;
        private readonly AttendanceService _sut;
        private const int ValidEmployeeId = 1;

        public AttendanceServiceTests()
        {
            _repoMock = new Mock<IAttendanceRepository>();
            _showCauseMock = new Mock<IShowCauseRepository>();

            // Default: employee exists and is active
            _repoMock.Setup(r => r.EmployeeExistsAsync(ValidEmployeeId))
                     .ReturnsAsync(true);

            _sut = new AttendanceService(_repoMock.Object, _showCauseMock.Object);
        }

        // ????????????????????????????????????????????????????????????
        // ? FIRST SIGN-IN TESTS (Success Cases)
        // ????????????????????????????????????????????????????????????

        [Fact]
        public async Task TC001_CheckIn_FirstTimeToday_ShouldSucceed()
        {
            // Arrange: No attendance record exists for today
            _repoMock.Setup(r => r.GetAttendanceAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync((Data.Entities.Attendance?)null);

            _repoMock.Setup(r => r.AddCheckInAsync(It.IsAny<Data.Entities.Attendance>()))
                     .Returns(Task.CompletedTask);

            // Act
            var act = async () => await _sut.CheckInAsync(ValidEmployeeId);

            // Assert
            await act.Should().NotThrowAsync(
                "a first check-in of the day must succeed without exceptions");

            _repoMock.Verify(r => r.AddCheckInAsync(
                It.Is<Data.Entities.Attendance>(a =>
                    a.EmployeeId == ValidEmployeeId &&
                    a.AttendanceDate == DateTime.Today &&
                    a.Status == "Present")),
                Times.Once,
                "AddCheckInAsync should be called exactly once with correct data");
        }

        // ????????????????????????????????????????????????????????????
        // ?? MULTIPLE SIGN-IN TESTS (Approval Required Cases)
        // ????????????????????????????????????????????????????????????

        [Fact]
        public async Task TC002_CheckIn_MultipleSignIn_ShouldRequireApproval()
        {
            // Arrange: Attendance record already exists for today
            var existingAttendance = new Data.Entities.Attendance
            {
                Id = 1,
                EmployeeId = ValidEmployeeId,
                AttendanceDate = DateTime.Today,
                CheckInTime = DateTime.Today.AddHours(9),
                Status = "Present"
            };

            _repoMock.Setup(r => r.GetAttendanceAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync(existingAttendance);

            // No pending request exists yet
            _showCauseMock.Setup(r => r.GetPendingByEmployeeAndDateAsync(ValidEmployeeId, DateTime.Today))
                          .ReturnsAsync((Data.Entities.ShowCauseRequest?)null);

            _showCauseMock.Setup(r => r.AddAsync(It.IsAny<Data.Entities.ShowCauseRequest>()))
                          .Returns((Task<ShowCauseRequest>)Task.CompletedTask);

            // Act
            var act = async () => await _sut.CheckInAsync(ValidEmployeeId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>(
                "multiple sign-in on the same day must be rejected")
                .WithMessage("*SHOW_CAUSE_REQUIRED*");

            // Verify approval request was created
            _showCauseMock.Verify(r => r.AddAsync(
                It.Is<Data.Entities.ShowCauseRequest>(s =>
                    s.EmployeeId == ValidEmployeeId &&
                    s.Status == "Pending" &&
                    s.Type == "LOGIN")),
                Times.Once,
                "A show cause request should be created for approval");
        }

        [Fact]
        public async Task TC003_CheckIn_WithPendingApproval_ShouldRejectWithMessage()
        {
            // Arrange: Attendance exists AND pending approval request already exists
            var existingAttendance = new Data.Entities.Attendance
            {
                Id = 1,
                EmployeeId = ValidEmployeeId,
                AttendanceDate = DateTime.Today,
                CheckInTime = DateTime.Today.AddHours(9),
                Status = "Present"
            };

            var pendingRequest = new Data.Entities.ShowCauseRequest
            {
                Id = 1,
                EmployeeId = ValidEmployeeId,
                Status = "Pending",
                RequestedAt = DateTime.Now
            };

            _repoMock.Setup(r => r.GetAttendanceAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync(existingAttendance);

            _showCauseMock.Setup(r => r.GetPendingByEmployeeAndDateAsync(ValidEmployeeId, DateTime.Today))
                          .ReturnsAsync(pendingRequest);

            // Act
            var act = async () => await _sut.CheckInAsync(ValidEmployeeId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>(
                "sign-in with pending approval should be rejected")
                .WithMessage("*PENDING_APPROVAL*");

            // Verify no new request was created
            _showCauseMock.Verify(r => r.AddAsync(It.IsAny<Data.Entities.ShowCauseRequest>()),
                Times.Never,
                "No new request should be created when one is already pending");
        }

        // ????????????????????????????????????????????????????????????
        // ? ERROR CASES
        // ????????????????????????????????????????????????????????????

        [Fact]
        public async Task TC004_CheckIn_UnknownEmployee_ShouldThrowKeyNotFoundException()
        {
            // Arrange: Employee doesn't exist
            const int unknownEmployeeId = 9999;
            _repoMock.Setup(r => r.EmployeeExistsAsync(unknownEmployeeId))
                     .ReturnsAsync(false);

            // Act
            var act = async () => await _sut.CheckInAsync(unknownEmployeeId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>(
                "check-in for a non-existent employee must be rejected")
                .WithMessage("*not found or is inactive*");
        }

        // ????????????????????????????????????????????????????????????
        // ?? HISTORY & RETRIEVAL TESTS
        // ????????????????????????????????????????????????????????????

        [Fact]
        public async Task TC005_GetAttendanceHistory_ShouldReturnRecords()
        {
            // Arrange
            var records = new List<Data.Entities.Attendance>
            {
                new()
                {
                    Id = 1,
                    EmployeeId = ValidEmployeeId,
                    AttendanceDate = DateTime.Today,
                    CheckInTime = DateTime.Today.AddHours(9),
                    Status = "Present"
                },
                new()
                {
                    Id = 2,
                    EmployeeId = ValidEmployeeId,
                    AttendanceDate = DateTime.Today.AddDays(-1),
                    CheckInTime = DateTime.Today.AddDays(-1).AddHours(9),
                    Status = "Present"
                }
            };

            _repoMock.Setup(r => r.GetHistoryAsync(ValidEmployeeId))
                     .ReturnsAsync(records);

            // Act
            var history = await _sut.GetAttendanceHistoryAsync(ValidEmployeeId);

            // Assert
            history.Should().HaveCount(2, "should return all attendance records");
            history[0].EmployeeId.Should().Be(ValidEmployeeId);
        }

        [Fact]
        public async Task TC006_GetAttendanceByDate_ShouldReturnRecord()
        {
            // Arrange
            var record = new Data.Entities.Attendance
            {
                Id = 1,
                EmployeeId = ValidEmployeeId,
                AttendanceDate = DateTime.Today,
                CheckInTime = DateTime.Today.AddHours(9),
                Status = "Present"
            };

            _repoMock.Setup(r => r.GetByDateAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync(record);

            // Act
            var result = await _sut.GetAttendanceByDateAsync(ValidEmployeeId, DateTime.Today);

            // Assert
            result.Should().NotBeNull();
            result!.CheckInTime.Should().Be(record.CheckInTime);
        }

        [Fact]
        public async Task TC007_GetAttendanceByDate_NoRecord_ShouldReturnNull()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByDateAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync((Data.Entities.Attendance?)null);

            // Act
            var result = await _sut.GetAttendanceByDateAsync(ValidEmployeeId, DateTime.Today);

            // Assert
            result.Should().BeNull("no record should be found for this date");
        }
    }
}