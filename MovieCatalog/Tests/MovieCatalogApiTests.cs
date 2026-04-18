using System.Net;
using System.Net.Http;
using MovieCatalog.Dtos;
using RestSharp;
using RestSharp.Authenticators;

namespace MovieCatalog.Tests;

[TestFixture]
public class MovieCatalogApiTests
{
    private const string BaseApiUrl = "http://144.91.123.158:5000/api/";
    private const string CreateMovieSuccessMessage = "Movie created successfully!";
    private const string EditMovieSuccessMessage = "Movie edited successfully!";
    private const string DeleteMovieSuccessMessage = "Movie deleted successfully!";
    private const string EditMissingMovieMessage = "Unable to edit the movie! Check the movieId parameter or user verification!";
    private const string DeleteMissingMovieMessage = "Unable to delete the movie! Check the movieId parameter or user verification!";

    private static string createdMovieId = string.Empty;
    private static RestClient? anonymousClient;
    private static RestClient? authorizedClient;

    private static RestClient CreateClient(string? accessToken = null)
    {
        var options = new RestClientOptions(BaseApiUrl)
        {
            ConfigureMessageHandler = handler =>
            {
                if (handler is HttpClientHandler httpClientHandler)
                {
                    httpClientHandler.UseProxy = false;
                    httpClientHandler.Proxy = null;
                }

                return handler;
            }
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            options.Authenticator = new JwtAuthenticator(accessToken);
        }

        return new RestClient(options);
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        anonymousClient = CreateClient();

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var password = "Pass123!";
        var email = $"movie.catalog.{uniqueSuffix}@example.com";

        var registerUser = new RegisterUserDto
        {
            UserName = $"movie{uniqueSuffix}",
            FirstName = "Movie",
            LastName = "Tester",
            Email = email,
            Password = password,
            RePassword = password
        };

        var registerRequest = new RestRequest("User/Register", Method.Post);
        registerRequest.AddJsonBody(registerUser);

        var registerResponse = await anonymousClient.ExecuteAsync<ApiResponseDto>(registerRequest);

        Assert.That(
            registerResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            $"User registration failed in test setup. ResponseStatus: {registerResponse.ResponseStatus}; ErrorMessage: {registerResponse.ErrorMessage}; Exception: {registerResponse.ErrorException?.Message}");

        var loginRequest = new RestRequest("User/Authentication", Method.Post);
        loginRequest.AddJsonBody(new LoginRequestDto
        {
            Email = email,
            Password = password
        });

        var loginResponse = await anonymousClient.ExecuteAsync<LoginResponseDto>(loginRequest);

        Assert.Multiple(() =>
        {
            Assert.That(
                loginResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                $"User authentication failed in test setup. ResponseStatus: {loginResponse.ResponseStatus}; ErrorMessage: {loginResponse.ErrorMessage}; Exception: {loginResponse.ErrorException?.Message}");
            Assert.That(loginResponse.Data, Is.Not.Null, "Login response body is missing.");
            Assert.That(loginResponse.Data!.AccessToken, Is.Not.Null.And.Not.Empty, "JWT token is missing from login response.");
        });

        authorizedClient = CreateClient(loginResponse.Data!.AccessToken);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        if (authorizedClient is not null && !string.IsNullOrWhiteSpace(createdMovieId))
        {
            var deleteRequest = new RestRequest("Movie/Delete", Method.Delete);
            deleteRequest.AddQueryParameter("movieId", createdMovieId);

            await authorizedClient.ExecuteAsync<ApiResponseDto>(deleteRequest);
            createdMovieId = string.Empty;
        }

        authorizedClient?.Dispose();
        anonymousClient?.Dispose();
    }

    [Test]
    [Order(1)]
    public async Task CreateMovieWithRequiredFields_ShouldCreateMovieSuccessfully()
    {
        var movieToCreate = new MovieDto
        {
            Title = $"Exam Movie {DateTime.UtcNow:yyyyMMddHHmmss}",
            Description = "Movie created during the API exam automation test.",
            PosterUrl = string.Empty,
            TrailerLink = string.Empty,
            IsWatched = true
        };

        var request = new RestRequest("Movie/Create", Method.Post);
        request.AddJsonBody(movieToCreate);

        var response = await authorizedClient!.ExecuteAsync<ApiResponseDto>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data!.Movie, Is.Not.Null);
            Assert.That(response.Data.Movie!.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(response.Data.Msg, Is.EqualTo(CreateMovieSuccessMessage));
        });

        createdMovieId = response.Data!.Movie!.Id;
    }

    [Test]
    [Order(2)]
    public async Task EditCreatedMovie_ShouldEditMovieSuccessfully()
    {
        Assert.That(createdMovieId, Is.Not.Null.And.Not.Empty, "Movie ID from the create test is missing.");

        var editedMovie = new MovieDto
        {
            Title = "Edited Exam Movie",
            Description = "Movie description updated by the ordered edit test.",
            PosterUrl = string.Empty,
            TrailerLink = string.Empty,
            IsWatched = false
        };

        var request = new RestRequest("Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", createdMovieId);
        request.AddJsonBody(editedMovie);

        var response = await authorizedClient!.ExecuteAsync<ApiResponseDto>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data!.Msg, Is.EqualTo(EditMovieSuccessMessage));
        });
    }

    [Test]
    [Order(3)]
    public async Task GetAllMovies_ShouldReturnNonEmptyCollection()
    {
        var request = new RestRequest("Catalog/All", Method.Get);

        var response = await authorizedClient!.ExecuteAsync<List<MovieDto>>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data, Is.Not.Empty);
        });
    }

    [Test]
    [Order(4)]
    public async Task DeleteCreatedMovie_ShouldDeleteMovieSuccessfully()
    {
        Assert.That(createdMovieId, Is.Not.Null.And.Not.Empty, "Movie ID from the create test is missing.");

        var request = new RestRequest("Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", createdMovieId);

        var response = await authorizedClient!.ExecuteAsync<ApiResponseDto>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data!.Msg, Is.EqualTo(DeleteMovieSuccessMessage));
        });

        createdMovieId = string.Empty;
    }

    [Test]
    [Order(5)]
    public async Task CreateMovieWithoutRequiredFields_ShouldReturnBadRequest()
    {
        var request = new RestRequest("Movie/Create", Method.Post);
        request.AddJsonBody(new { });

        var response = await authorizedClient!.ExecuteAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    [Order(6)]
    public async Task EditNonExistingMovie_ShouldReturnBadRequestWithExpectedMessage()
    {
        var request = new RestRequest("Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", Guid.Empty.ToString());
        request.AddJsonBody(new MovieDto
        {
            Title = "Ghost Movie",
            Description = "This movie does not exist and should not be edited.",
            PosterUrl = string.Empty,
            TrailerLink = string.Empty,
            IsWatched = false
        });

        var response = await authorizedClient!.ExecuteAsync<ApiResponseDto>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data!.Msg, Is.EqualTo(EditMissingMovieMessage));
        });
    }

    [Test]
    [Order(7)]
    public async Task DeleteNonExistingMovie_ShouldReturnBadRequestWithExpectedMessage()
    {
        var request = new RestRequest("Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", Guid.Empty.ToString());

        var response = await authorizedClient!.ExecuteAsync<ApiResponseDto>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data!.Msg, Is.EqualTo(DeleteMissingMovieMessage));
        });
    }
}
