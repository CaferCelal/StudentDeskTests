using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using StudentDesk.Model;
using StudentDesk;
using Moq;
using System.Collections.Generic;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace StudentDesk.Tests
{
    [TestClass]
    public class BaseDaoTests {
        private Helper _helper;
        private IConfiguration _configuration;
        private BaseDao _dao;
        private readonly string _connectionString = "Server=tcp:evrenuzsoftworks.database.windows.net,1433;Initial Catalog=studesk;Persist Security Info=False;User ID=CaferEvrenuz;Password=MyPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"; 

        [TestInitialize]
        public void Setup()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "ConnectionStrings:MyDatabaseConnection", _connectionString }
                })
                .Build();

            _dao = new BaseDao(configuration);
            _helper = new Helper(configuration);
            _dao.ConnectionOpen();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _dao.ConnectionClose();
        }

        [TestMethod]
        public void CheckEmailExists_EmailExists_ReturnsTrue()
        {
            // Arrange
            string testEmail = "test@example.com";
            string testTable = "Students";

            // Act
            bool exists = _dao.CheckEmailExists(testEmail, testTable);

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public void CheckEmailExists_EmailDoesNotExist_ReturnsFalse()
        {
            // Arrange
            string testEmail = "nonexistent@example.com";
            string testTable = "Students";

            // Act
            bool exists = _dao.CheckEmailExists(testEmail, testTable);

            // Assert
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public void ComparePasswords_CorrectPassword_ReturnsTrue()
        {
            // Arrange
            string testEmail = "celal.evrenuz@gmail.com";
            string testPassword = "1ec3ecb2aade1100c94c79d0b54bc545ab60443c401eb32f6d45e310f1b0cc21";
            string testTable = "Philanthropists";

            // Act
            bool match = _dao.ComparePasswords(testEmail, testPassword, testTable);

            // Assert
            Assert.IsTrue(match);
        }

        [TestMethod]
        public void ComparePasswords_IncorrectPassword_ReturnsFalse()
        {
            // Arrange
            string testEmail = "test@example.com";
            string testPassword = "WrongPassword!";
            string testTable = "Students";

            // Act
            bool match = _dao.ComparePasswords(testEmail, testPassword, testTable);

            // Assert
            Assert.IsFalse(match);
        }

        [TestMethod]
        public void RetrieveSalt_ExistingEmail_ReturnsSalt()
        {
            // Arrange
            string testEmail = "test@example.com";
            string testTable = "Students";

            // Act
            string salt = _dao.RetrieveSalt(testEmail, testTable);

            // Assert
            Assert.IsNotNull(salt);
        }

        [TestMethod]
        public void RetrieveSalt_NonExistingEmail_ReturnsNull()
        {
            // Arrange
            string testEmail = "nonexistent@example.com";
            string testTable = "Students";

            // Act
            string salt = _dao.RetrieveSalt(testEmail, testTable);

            // Assert
            Assert.IsNull(salt);
        }

        [TestMethod]
        public void UpdateVerificationCode_ValidEmail_UpdatesCode()
        {
            // Arrange
            string testEmail = "test@example.com";
            string verificationCode = "654321";
            string testTable = "Students";

            // Act
            _dao.UpdateVerificationCode(testEmail, verificationCode, testTable);

            // Query the database to verify the code has been updated
            string sqlQuery = $"SELECT Verification_Code FROM {testTable} WHERE Email = @Email";
            string updatedCode = null;

            using (SqlCommand cmd = new SqlCommand(sqlQuery, _dao._SqlConnection))
            {
                cmd.Parameters.AddWithValue("@Email", testEmail);
                _dao.ConnectionOpen();
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    updatedCode = result.ToString();
                }
                _dao.ConnectionClose();
            }

            // Assert
            Assert.AreEqual(verificationCode, updatedCode);
        }

        [TestMethod]
        public void UpdateSalt_ValidEmail_UpdatesSalt()
        {
            // Arrange
            string testEmail = "test@example.com";
            string newSalt = "NewSaltValue";
            string testTable = "Students";

            // Act
            _dao.UpdateSalt(testEmail, newSalt, testTable);

            // Query the database to verify the salt has been updated
            string sqlQuery = $"SELECT Salt FROM {testTable} WHERE Email = @Email";
            string updatedSalt = null;

            using (SqlCommand cmd = new SqlCommand(sqlQuery, _dao._SqlConnection))
            {
                cmd.Parameters.AddWithValue("@Email", testEmail);
                _dao.ConnectionOpen();
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    updatedSalt = result.ToString();
                }
                _dao.ConnectionClose();
            }

            // Assert
            Assert.AreEqual(newSalt, updatedSalt);
        }

        [TestMethod]
        public void Authenticate_ValidCredentials_ReturnsTrue()
        {
            // Arrange
            string testEmail = "celal.evrenuz@gmail.com";
            string testPassword = "Lenovo";  // The original password before hashing
            string testTable = "Philanthropists";
            

            // Act
            bool authenticated = _dao.Authenticate(testEmail, testPassword, testTable);

            // Assert
            Assert.IsTrue(authenticated);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void Authenticate_InvalidCredentials_ThrowsException()
        {
            // Arrange
            string testEmail = "test@example.com";
            string testPassword = "WrongPassword!";
            string testTable = "Students";

            // Act
            _dao.Authenticate(testEmail, testPassword, testTable);
        }

        [TestMethod]
        public void VerifyVerificationCode_ValidCode_ReturnsTrue()
        {
            // Arrange
            string testEmail = "test@example.com";
            string verificationCode = "654321";
            string testTable = "Students";

            // Act
            bool isVerified = _dao.VerifyVerificationCode(testEmail, verificationCode, testTable);

            // Assert
            Assert.IsTrue(isVerified);
        }

        [TestMethod]
        public void VerifyVerificationCode_InvalidCode_ReturnsFalse()
        {
            // Arrange
            string testEmail = "test@example.com";
            string verificationCode = "123456";
            string testTable = "Students";

            // Act
            bool isVerified = _dao.VerifyVerificationCode(testEmail, verificationCode, testTable);

            // Assert
            Assert.IsFalse(isVerified);
        }

        [TestMethod]
        public void UpdatePasswordViaEmail_ValidRequest_UpdatesPassword()
        {
            // Arrange
            string testEmail = "test@example.com";
            string newPassword = "NewPassword123!";
            string verificationCode = "654321";
            string testTable = "Students";

            // Act
            bool updated = _dao.UpdatePasswordViaEmail(testEmail, newPassword, verificationCode, testTable);
            bool isAuthenticated = _dao.Authenticate(testEmail, newPassword, testTable);

            // Assert
            Assert.IsTrue(updated);
            Assert.IsTrue(isAuthenticated);
        }

        [TestMethod]
        public void GetIdViaEmail_ValidEmail_ReturnsUserId()
        {
            // Arrange
            string testEmail = "test@example.com";
            string testTable = "Students";

            // Act
            int userId = _dao.GetIdViaEmail(testEmail, testTable);

            // Assert
            Assert.AreEqual(13, userId);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void GetIdViaEmail_InvalidEmail_ThrowsException()
        {
            // Arrange
            string testEmail = "nonexistent@example.com";
            string testTable = "Students";

            // Act
            int userId = _dao.GetIdViaEmail(testEmail, testTable);
        }

        [TestMethod]
        public void AddRequest_ValidRequest_AddsToDatabase()
        {
            // Arrange
            string testTable = "Student_Requests";
            var request = new Request
            {
                Email = "test@example.com",
                RequestType = "Inquiry",
                RequestSubType = "Information",
                request = "Please provide information about the course."
            };

            // Act
            _dao.AddRequest(testTable, request);
            _dao.ConnectionOpen();

            // Verify the request was added by checking the RequestType
            string sqlQuery = $"SELECT Request_Type FROM {testTable} WHERE Email = @Email";
            string retrievedRequestType = null;

            using (SqlCommand cmd = new SqlCommand(sqlQuery, _dao._SqlConnection))
            {
                cmd.Parameters.AddWithValue("@Email", request.Email);
                retrievedRequestType = (string)cmd.ExecuteScalar();
            }

            _dao.ConnectionClose();

            // Assert
            Assert.AreEqual("Inquiry", retrievedRequestType);
        }
    }
}
