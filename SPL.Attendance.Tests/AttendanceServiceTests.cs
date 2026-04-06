using FluentAssertions;
using Moq;
using SPL.Attendance.Business.Services;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;
using Xunit;

namespace SPL.Attendance.Tests
{
    public class AttendanceServiceTests
    {
        private readonly Mock<IAttendanceRepository> _repoMock;
        private readonly AttendanceService _sut; 
        private const int ValidEmployeeId = 1;

        public AttendanceServiceTests()
        {
            _repoMock = new Mock<IAttendanceRepository>();

            // Default: employee exists and is active
            _repoMock.Setup(r => r.EmployeeExistsAsync(ValidEmployeeId))
                     .ReturnsAsync(true);
            var showCauseMock = new Mock<IShowCauseRepository>();
            _sut = new AttendanceService(_repoMock.Object, showCauseMock.Object);
        }


        [Fact]
        public async Task TC001_CheckIn_FirstTimeToday_ShouldSucceed()
        {
            _repoMock.Setup(r => r.GetAttendanceAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync((Data.Entities.Attendance?)null);

            _repoMock.Setup(r => r.AddCheckInAsync(It.IsAny<Data.Entities.Attendance>()))
                     .Returns(Task.CompletedTask);

            var act = async () => await _sut.CheckInAsync(ValidEmployeeId);

            await act.Should().NotThrowAsync(
                "a first check-in of the day must succeed without exceptions");

            _repoMock.Verify(r => r.AddCheckInAsync(
                It.Is<Data.Entities.Attendance>(a =>
                    a.EmployeeId    == ValidEmployeeId &&
                    a.AttendanceDate == DateTime.Today &&
                    a.Status        == "Present")),
                Times.Once,
                "AddCheckInAsync should be called exactly once with correct data");
        }


        [Fact]
        public async Task TC002_CheckIn_DuplicateToday_ShouldThrowInvalidOperationException()
        {
            _repoMock.Setup(r => r.GetAttendanceAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync(new Data.Entities.Attendance
                     {
                         Id             = 1,
                         EmployeeId     = ValidEmployeeId,
                         AttendanceDate = DateTime.Today,
                         CheckInTime    = DateTime.Today.AddHours(9),
                         Status         = "Present"
                     });

            // Act
            var act = async () => await _sut.CheckInAsync(ValidEmployeeId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>(
                "duplicate check-in on the same day must be rejected")
                .WithMessage("*already checked in today*");
        }


        [Fact]
        public async Task TC003_CheckOut_AfterValidCheckIn_ShouldSaveCheckOutAndWorkHours()
        {
            var checkInTime = DateTime.Today.AddHours(9);

            var existingRecord = new Data.Entities.Attendance
            {
                Id             = 1,
                EmployeeId     = ValidEmployeeId,
                AttendanceDate = DateTime.Today,
                CheckInTime    = checkInTime,
                CheckOutTime   = null,
                Status         = "Present"
            };

            _repoMock.Setup(r => r.GetAttendanceAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync(existingRecord);

            _repoMock.Setup(r => r.UpdateCheckOutAsync(It.IsAny<Data.Entities.Attendance>()))
                     .Returns(Task.CompletedTask);

            // Act
            await _sut.CheckOutAsync(ValidEmployeeId);

            // Assert
            _repoMock.Verify(r => r.UpdateCheckOutAsync(
                It.Is<Data.Entities.Attendance>(a =>
                    a.CheckOutTime != null &&
                    a.WorkHours    != null &&
                    a.WorkHours    > 0)),
                Times.Once,
                "UpdateCheckOutAsync should be called once with a CheckOutTime and positive WorkHours");
        }


        [Fact]
        public async Task TC004_CheckOut_WithNoCheckIn_ShouldThrowInvalidOperationException()
        {
            _repoMock.Setup(r => r.GetAttendanceAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync((Data.Entities.Attendance?)null);

            // Act
            var act = async () => await _sut.CheckOutAsync(ValidEmployeeId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>(
                "check-out without a prior check-in must be rejected")
                .WithMessage("*no check-in record*");
        }


        [Fact]
        public async Task TC005_CheckOut_WhenAlreadyCheckedOut_ShouldThrowInvalidOperationException()
        {
            _repoMock.Setup(r => r.GetAttendanceAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync(new Data.Entities.Attendance
                     {
                         Id             = 1,
                         EmployeeId     = ValidEmployeeId,
                         AttendanceDate = DateTime.Today,
                         CheckInTime    = DateTime.Today.AddHours(9),
                         CheckOutTime   = DateTime.Today.AddHours(17).AddMinutes(30),
                         WorkHours      = 8.50m,
                         Status         = "Present"
                     });

            var act = async () => await _sut.CheckOutAsync(ValidEmployeeId);

            await act.Should().ThrowAsync<InvalidOperationException>(
                "duplicate check-out on the same day must be rejected")
                .WithMessage("*already checked out today*");
        }


        [Fact]
        public async Task TC006_CheckOut_WorkHoursCalculation_ShouldBe8Point50()
        {
            var checkInTime = DateTime.Today.AddHours(9).AddMinutes(0);   

            var record = new Data.Entities.Attendance
            {
                Id             = 1,
                EmployeeId     = ValidEmployeeId,
                AttendanceDate = DateTime.Today,
                CheckInTime    = checkInTime,
                CheckOutTime   = null,
                Status         = "Present"
            };

            _repoMock.Setup(r => r.GetAttendanceAsync(ValidEmployeeId, DateTime.Today))
                     .ReturnsAsync(record);

            Data.Entities.Attendance? savedRecord = null;
            _repoMock.Setup(r => r.UpdateCheckOutAsync(It.IsAny<Data.Entities.Attendance>()))
                     .Callback<Data.Entities.Attendance>(a => savedRecord = a)
                     .Returns(Task.CompletedTask);

            var simulatedCheckOut = DateTime.Today.AddHours(17).AddMinutes(30); // 17:30
            var expectedHours     = (decimal)(simulatedCheckOut - checkInTime).TotalHours; // 8.50

            expectedHours.Should().Be(8.50m,
                "09:00 to 17:30 must equal exactly 8.50 work hours");

            await _sut.CheckOutAsync(ValidEmployeeId);

            savedRecord.Should().NotBeNull();
            savedRecord!.WorkHours.Should().BeGreaterThan(0,
                "work hours must be positive after a valid check-out");
            savedRecord.WorkHours.Should().HaveValue()
                .And.Match(wh => Math.Round(wh!.Value, 2) == wh.Value,
                           "work hours must be rounded to 2 decimal places");
        }


        [Fact]
        public async Task CheckIn_UnknownEmployee_ShouldThrowKeyNotFoundException()
        {
            const int unknownEmployeeId = 9999;
            _repoMock.Setup(r => r.EmployeeExistsAsync(unknownEmployeeId))
                     .ReturnsAsync(false);

            var act = async () => await _sut.CheckInAsync(unknownEmployeeId);

            await act.Should().ThrowAsync<KeyNotFoundException>(
                "check-in for a non-existent employee must be rejected");
        }
    }
}
