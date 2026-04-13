using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Net;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using ExamPrepIdeaCenter.Models;


namespace ExamPrepIdeaCenter
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://144.91.123.158:82";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIzNTM5OTA0Yy1lMTBlLTQ4NjMtYWJmYy0zYjA1NDc4ZmJiNGIiLCJpYXQiOiIwNC8xMy8yMDI2IDA4OjEyOjQ3IiwiVXNlcklkIjoiZjA1YmVmNjYtNDZkYS00MTU2LTUzNDUtMDhkZTc2YTJkM2VjIiwiRW1haWwiOiJtaWVsZTI2QGV4YW1wbGUuY29tIiwiVXNlck5hbWUiOiJtaWVsZTI2IiwiZXhwIjoxNzc2MDg5NTY3LCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9.ar2O5TiPquulvgtTyti3ITJXnDITjIy5V9b-rTY80IE";
        private const string LoginEmail = "miele26@example.com";
        private const string LoginPassword = "1234567";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;
            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }
            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);
        }
        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new {email, password});

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in response");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException("Response status code was not OK");
            }
            
        }
        [Order(1)]
        [Test]
        public void createIdeaWithRequiredFieldsShouldReturnSuccess()
        {
            var ideaRequest = new IdeaDTO{ Title = "First Idea", Description = "Interesting", Url = ""};
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);

            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnSuccess()
        {
            var idea = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(idea);
            var createResponse = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode == HttpStatusCode.OK, "Status code 200");
            Assert.That(createResponse, Is.Not.Empty);
            Assert.That(createResponse, Is.Not.Null);
            lastCreatedIdeaId = createResponse.Last().Id;
        }

        [Order(3)]
        [Test]
        public void EditLastIdea_ShouldReturnSuccess()
        {
            var idea = new RestRequest("/api/Idea/Edit", Method.Put);
            idea.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var body = new IdeaDTO { Title  = " New Name", Url = "", Description = "Updated"};
            idea.AddJsonBody(body);
            var response = this.client.Execute(idea);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode == HttpStatusCode.OK, "Status code 200");
            Assert.That(createResponse.Msg, Is.EqualTo("Edited successfully"));
        }
        [Order(4)]
        [Test]
        public void deleteIdea_ShouldReturnSuccess()
        {
            var idea = new RestRequest("/api/Idea/Delete", Method.Delete);
            idea.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(idea);

            Assert.That(response.StatusCode == HttpStatusCode.OK, "Status code 200");
            Assert.That(response.Content, Is.EqualTo("\"The idea is deleted!\""));
        }

        [Order(5)]
        [Test]
        public void createIdeaWithMissingRequiredFieldsShouldReturnBadRequest()
        {
            var ideaRequest = new IdeaDTO { Title = "", Description = "", Url = "" };
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400");
        }

        [Order(6)]
        [Test]
        public void EditNonExistingIdea_ShouldReturnBadRequest()
        {
            var idea = new RestRequest("/api/Idea/Edit", Method.Put);
            idea.AddQueryParameter("ideaId", -5);
            var body = new IdeaDTO { Title = " New Name", Url = "", Description = "Updated" };
            idea.AddJsonBody(body);
            var response = this.client.Execute(idea);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400");
            Assert.That(response.Content, Is.EqualTo("\"There is no such idea!\""));
        }

        [Order(7)]
        [Test]
        public void deleteNonExistingIdea_ShouldReturnBadRequest()
        {
            var idea = new RestRequest("/api/Idea/Delete", Method.Delete);
            idea.AddQueryParameter("ideaId", -3);
            var response = this.client.Execute(idea);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400");
            Assert.That(response.Content, Is.EqualTo("\"There is no such idea!\""));
        }
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            this.client.Dispose();
        }
    }
}
