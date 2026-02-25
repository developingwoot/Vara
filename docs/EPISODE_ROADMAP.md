# VARA: 12-Episode Development Roadmap
## Monthly "Building in Public" Video Series

**Format:** 15-25 minute monthly videos showing real progress, learnings, and challenges  
**Total Duration:** 12 months from conception to MVP  
**Designed For:** ~20 hrs/month development + video production  
**Release Schedule:** First Thursday of each month (consistent cadence)

---

## Quick Reference

| Episode | Month | Phase | Focus | Time Est. | Deliverable |
|---------|-------|-------|-------|-----------|-------------|
| 1 | 1 | Foundations | Setup, Auth, Database | 25 hrs | Working auth endpoints |
| 2 | 2 | Foundations | YouTube API, Data Layer | 20 hrs | Video fetching + caching |
| 3 | 3 | Foundations | API Endpoints, Validation | 18 hrs | REST API operational |
| 4 | 4 | Analysis | Keyword Research Service | 20 hrs | Keyword scoring working |
| 5 | 5 | Analysis | Video Analysis Service | 22 hrs | Pattern detection working |
| 6 | 6 | Analysis | Trend Detection Service | 20 hrs | Trend calculation working |
| 7 | 7 | LLM | Multi-Provider Setup | 25 hrs | Multiple LLMs callable |
| 8 | 8 | LLM | LLM + Keyword Analysis | 20 hrs | LLM insights integrated |
| 9 | 9 | LLM | Transcripts + LLM | 22 hrs | Transcript analysis working |
| 10 | 10 | Advanced | Plugin Architecture | 25 hrs | Plugins discoverable/extensible |
| 11 | 11 | Frontend | Web UI + Real-Time | 28 hrs | Functioning dashboard |
| 12 | 12 | Polish | Optimization + Launch | 20 hrs | Production-ready MVP |

**Bonus Episodes (when extra time available):**
- Bonus A: Performance Optimization (15 hrs)
- Bonus B: Mock Billing UI (18 hrs)
- Bonus C: Deployment Guide (20 hrs)

---

---

## PHASE 1: BACKEND FOUNDATION (Months 1-3)

---

## Episode 1: Project Setup, Database, Authentication
**Month:** 1  
**Time Estimate:** 25 hours (heavier—foundational)  
**Complexity:** Medium (new project setup)

### What Gets Built
- .NET 10 minimal API project scaffolding
- PostgreSQL database design + Entity Framework Core
- User entity with JWT-based authentication
- Password hashing (bcrypt)
- Basic error handling and structured logging
- Docker Compose for local development
- GitHub Actions secrets configuration
- API health check endpoints

### Key Technical Concepts
- .NET dependency injection configuration
- Entity Framework Core migrations
- JWT token generation and validation
- Password hashing and salt
- CORS for frontend integration
- Docker containerization
- Environment-based configuration (.env files)

### Code Deliverables
```csharp
// User.cs entity
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public string SubscriptionTier { get; set; } = "free";
    public DateTime CreatedAt { get; set; }
}

// TokenService.cs
public class TokenService
{
    public string GenerateToken(User user)
    {
        var claims = new[] { 
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email)
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"])),
                SecurityAlgorithms.HmacSha256)
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// AuthController.cs endpoints
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        // Hash password, create user, return JWT
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // Verify credentials, return JWT
    }
}
```

### Docker Compose Configuration
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: vara
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  api:
    build: .
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Database=vara;Username=postgres;Password=postgres
      Jwt__Secret: ${JWT_SECRET}
    ports:
      - "5000:5000"
    depends_on:
      - postgres

volumes:
  postgres_data:
```

### Testing Checklist
- [ ] Create user with POST /api/auth/register
- [ ] Login with correct password returns JWT token
- [ ] Login with wrong password fails with 401
- [ ] JWT token expires after 24 hours
- [ ] Protected endpoints reject requests without token
- [ ] PostgreSQL persists user data after container restart
- [ ] Health check endpoint returns 200 OK
- [ ] Docker Compose runs with single command

### Video Content
**Intro (2 min):** Show the spec, explain why starting with auth + database matters  
**Code-Along (15 min):** Live code authentication, show Entity Framework migration  
**Test (5 min):** Register user, login, verify database, test protected endpoint  
**Recap (2 min):** "Next: YouTube API integration"

### Common Challenges
- EF Core migrations complexity (show why they matter)
- JWT claim structure (explain what goes in token)
- CORS configuration (show how frontend will connect)

---

## Episode 2: YouTube API Client & Data Layer
**Month:** 2  
**Time Estimate:** 20 hours (standard)  
**Complexity:** Medium (external API integration)

### What Gets Built
- YouTubeClient wrapper around Google APIs
- Video entity and persistence layer
- Keyword entity and repository
- API error handling and retry logic
- Rate limiting (respecting YouTube API quotas)
- Structured logging for debugging
- Caching layer for API responses

### Key Technical Concepts
- RESTful API client patterns
- Abstraction layers (IYouTubeClient interface)
- Data mapping (API response → Entity)
- Rate limiting algorithms
- Exponential backoff for retries
- API quota management
- Dependency injection of external services

### Code Deliverables
```csharp
// IYouTubeClient.cs interface
public interface IYouTubeClient
{
    Task<List<VideoMetadata>> SearchAsync(
        string keyword,
        int maxResults = 10,
        CancellationToken ct = default);
        
    Task<VideoMetadata> GetVideoAsync(
        string videoId,
        CancellationToken ct = default);
        
    Task<string> GetTranscriptAsync(
        string videoId,
        CancellationToken ct = default);
}

// YouTubeClient.cs implementation
public class YouTubeClient : IYouTubeClient
{
    private readonly ILogger<YouTubeClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    
    public async Task<List<VideoMetadata>> SearchAsync(
        string keyword,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://www.googleapis.com/youtube/v3/search?" +
            $"q={Uri.EscapeDataString(keyword)}&" +
            $"key={_apiKey}&" +
            $"maxResults={maxResults}&" +
            $"part=snippet&" +
            $"type=video";
            
        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(ct);
        var searchResult = JsonConvert.DeserializeObject<YouTubeSearchResponse>(json);
        
        return searchResult.Items.Select(item => new VideoMetadata
        {
            YoutubeId = item.Id.VideoId,
            Title = item.Snippet.Title,
            Description = item.Snippet.Description,
            ThumbnailUrl = item.Snippet.Thumbnails.High.Url,
            ChannelName = item.Snippet.ChannelTitle,
            PublishedAt = item.Snippet.PublishedAt
        }).ToList();
    }
}

// Video.cs entity
public class Video
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string YoutubeId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ChannelName { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime UploadDate { get; set; }
    public long ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public string TranscriptText { get; set; }
    public DateTime MetadataFetchedAt { get; set; }
}
```

### Video Metadata Cache
```csharp
public class VideoCache
{
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "video_";
    private const int CacheDurationHours = 24;
    
    public async Task<VideoMetadata> GetOrFetchAsync(
        string videoId,
        Func<string, Task<VideoMetadata>> fetcher)
    {
        var cacheKey = $"{CacheKeyPrefix}{videoId}";
        
        if (_cache.TryGetValue(cacheKey, out VideoMetadata cached))
        {
            return cached;
        }
        
        var metadata = await fetcher(videoId);
        _cache.Set(cacheKey, metadata, 
            TimeSpan.FromHours(CacheDurationHours));
            
        return metadata;
    }
}
```

### Testing Checklist
- [ ] Search "Python tutorial" returns results from YouTube
- [ ] Metadata matches what's on YouTube (title, description, channel)
- [ ] Transcript fetching works for video with captions
- [ ] Rate limiter prevents API quota exhaustion
- [ ] Failed requests retry with exponential backoff
- [ ] Duplicate searches use cache (no new API call)
- [ ] Invalid video ID returns appropriate error
- [ ] Logging shows all API calls and latencies

### Video Content
**Intro (2 min):** Why API clients are critical, show what data we need  
**Code-Along (15 min):** Build YouTubeClient, show caching, handle errors  
**Test (5 min):** Search for keyword, verify results, show cache hit  
**Recap (2 min):** "Next: REST endpoints and validation"

### Common Challenges
- Handling YouTube API authentication complexity
- Understanding response structure and mapping
- Rate limiting and quota management
- Error handling for various failure modes

---

## Episode 3: Basic API Endpoints & Caching
**Month:** 3  
**Time Estimate:** 18 hours (lighter—mostly wiring)  
**Complexity:** Low (integration work)

### What Gets Built
- REST endpoints for video search/fetch
- Input validation (FluentValidation)
- API response DTOs (clean contracts)
- Swagger/OpenAPI documentation
- Basic error responses with standard format
- Caching at service layer (PostgreSQL + in-memory)
- CORS configuration for frontend

### Key Technical Concepts
- REST API design principles
- Data transfer objects (DTOs)
- Input validation frameworks
- API documentation (OpenAPI/Swagger)
- Error response standardization
- Layered architecture (controller → service → repository)

### Code Deliverables
```csharp
// VideosController.cs
[ApiController]
[Route("api/videos")]
[Authorize]
public class VideosController : ControllerBase
{
    private readonly IVideoService _videoService;
    
    [HttpPost("search")]
    [ProducesResponseType(typeof(List<VideoSearchResponse>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Search(
        [FromBody] VideoSearchRequest request)
    {
        var userId = User.FindFirst("sub")?.Value;
        var results = await _videoService.SearchAsync(
            request.Keyword,
            userId);
        return Ok(results);
    }
    
    [HttpGet("{youtubeId}")]
    [ProducesResponseType(typeof(VideoDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetVideo(string youtubeId)
    {
        var userId = User.FindFirst("sub")?.Value;
        var video = await _videoService.GetVideoAsync(youtubeId, userId);
        if (video == null) return NotFound();
        return Ok(video);
    }
}

// DTOs
public class VideoSearchRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Keyword { get; set; }
    
    public int MaxResults { get; set; } = 20;
}

public class VideoSearchResponse
{
    public string YoutubeId { get; set; }
    public string Title { get; set; }
    public string ChannelName { get; set; }
    public DateTime PublishedAt { get; set; }
    public string ThumbnailUrl { get; set; }
}

// Fluent Validation
public class VideoSearchRequestValidator : AbstractValidator<VideoSearchRequest>
{
    public VideoSearchRequestValidator()
    {
        RuleFor(x => x.Keyword)
            .NotEmpty().WithMessage("Keyword is required")
            .MinimumLength(2).WithMessage("Keyword must be at least 2 characters")
            .MaximumLength(100).WithMessage("Keyword must not exceed 100 characters");
            
        RuleFor(x => x.MaxResults)
            .GreaterThan(0).WithMessage("MaxResults must be greater than 0")
            .LessThanOrEqualTo(50).WithMessage("MaxResults must not exceed 50");
    }
}

// Program.cs Swagger setup
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "VARA API", 
        Version = "v1",
        Description = "Video Analyzer Research Assistant API"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});
```

### Error Response Format
```csharp
public class ErrorResponse
{
    public string Code { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string[]> Errors { get; set; }
}

// Middleware for consistent error handling
app.UseExceptionHandler((app) =>
{
    app.Run(async context =>
    {
        var exceptionHandler = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandler?.Error;
        
        var response = new ErrorResponse
        {
            Code = "INTERNAL_ERROR",
            Message = exception?.Message ?? "An error occurred"
        };
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(response);
    });
});
```

### Testing Checklist
- [ ] Search endpoint returns videos for valid keyword
- [ ] Second search for same keyword is much faster (cached)
- [ ] Invalid keyword returns 400 with validation error
- [ ] Unauthenticated requests return 401
- [ ] Swagger UI is complete and functional
- [ ] Error responses follow standard format
- [ ] Response times under 500ms for cached results
- [ ] CORS headers present for frontend domain

### Video Content
**Intro (2 min):** Why clean APIs matter, show what endpoints are built  
**Code-Along (12 min):** Build controllers, add validation, setup Swagger  
**Test (7 min):** Call endpoints, show Swagger UI, verify caching  
**Recap (2 min):** "Phase 1 done! Next phase: analysis services"

### Common Challenges
- Validation complexity (showing good error messages)
- Caching strategy (when to invalidate)
- CORS configuration (debugging browser errors)

---

---

## PHASE 2: ANALYSIS SERVICES (Months 4-6)

---

## Episode 4: Keyword Research Service
**Month:** 4  
**Time Estimate:** 20 hours (standard)  
**Complexity:** Medium (domain logic)

### What Gets Built
- KeywordAnalyzer service (pure business logic)
- Scoring algorithms (volume, competition, trend)
- Keyword intent classification
- Results caching (7-day TTL)
- REST endpoint for keyword analysis
- Unit tests for scoring logic
- Database storage of results

### Key Technical Concepts
- Domain-driven service design
- Algorithm implementation and testing
- Data normalization (0-100 scales)
- Service layer architecture
- Unit testing with xUnit
- Database result caching

### Scoring Algorithms
```csharp
public class KeywordAnalysisService
{
    public async Task<KeywordAnalysis> AnalyzeAsync(
        string keyword,
        string niche = null)
    {
        // Check cache first
        var cached = await _cache.GetAnalysisAsync(keyword, niche);
        if (cached != null) return cached;
        
        // Fetch top 10 videos for this keyword
        var topVideos = await _youtubeClient.SearchAsync(keyword, maxResults: 10);
        
        var analysis = new KeywordAnalysis
        {
            Keyword = keyword,
            Niche = niche,
            SearchVolume = CalculateSearchVolume(topVideos),
            CompetitionScore = CalculateCompetition(topVideos),
            TrendDirection = CalculateTrend(topVideos),
            KeywordIntent = ClassifyIntent(keyword),
            AnalyzedAt = DateTime.UtcNow
        };
        
        // Cache for 7 days
        await _cache.SetAnalysisAsync(analysis, TimeSpan.FromDays(7));
        
        return analysis;
    }
    
    private int CalculateSearchVolume(List<Video> videos)
    {
        // Volume based on total views of top videos
        var totalViews = videos.Sum(v => v.ViewCount);
        // Normalize to 0-100 scale
        return Math.Min((int)(totalViews / 1_000_000), 100);
    }
    
    private int CalculateCompetition(List<Video> videos)
    {
        // Competition = engagement + video age
        // High engagement + old videos = saturated keyword
        var avgEngagement = videos.Average(v =>
        {
            var total = (double)(v.LikeCount + v.CommentCount);
            var views = v.ViewCount > 0 ? v.ViewCount : 1;
            return (total / views) * 100;
        });
        
        var avgAge = videos.Average(v =>
            (DateTime.UtcNow - v.UploadDate).TotalDays);
            
        var ageScore = Math.Min((int)(avgAge / 10), 50);
        
        return (int)Math.Min(avgEngagement + ageScore, 100);
    }
    
    private string CalculateTrend(List<Video> videos)
    {
        // Simple trend: are recent videos getting more views?
        var recent = videos.Where(v => 
            (DateTime.UtcNow - v.UploadDate).TotalDays < 30)
            .Average(v => v.ViewCount);
            
        var older = videos.Where(v =>
            (DateTime.UtcNow - v.UploadDate).TotalDays >= 30)
            .Average(v => v.ViewCount);
            
        if (older == 0) return "new";
        var growth = (recent - older) / older;
        
        return growth > 0.2 ? "rising" : 
               growth < -0.2 ? "declining" : 
               "flat";
    }
    
    private string ClassifyIntent(string keyword)
    {
        // Simple keyword intent classification
        keyword = keyword.ToLower();
        
        return keyword switch
        {
            _ when keyword.Contains("tutorial") || 
                   keyword.Contains("how to") => "how-to",
            _ when keyword.Contains("review") => "opinion",
            _ when keyword.Contains("news") => "news",
            _ when keyword.Contains("best") => "entertainment",
            _ => "educational"
        };
    }
}
```

### Unit Tests
```csharp
[Fact]
public async Task AnalyzeAsync_ReturnsNormalizedScores()
{
    // Arrange
    var mockVideos = new List<Video>
    {
        new() { ViewCount = 10_000_000, LikeCount = 50_000, CommentCount = 5_000, UploadDate = DateTime.UtcNow.AddDays(-30) }
    };
    
    _youtubeClientMock.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockVideos);
    
    // Act
    var result = await _service.AnalyzeAsync("python tutorial");
    
    // Assert
    Assert.InRange(result.SearchVolume, 0, 100);
    Assert.InRange(result.CompetitionScore, 0, 100);
    Assert.NotNull(result.TrendDirection);
}
```

### Testing Checklist
- [ ] Analyze "Python tutorial" returns scores 0-100
- [ ] Search volume reflects actual view counts
- [ ] Competition score considers engagement and age
- [ ] Trend calculation shows actual direction
- [ ] Intent classification matches keyword
- [ ] Same keyword analyzed twice uses cache
- [ ] Results stored in database
- [ ] Cache expires after 7 days

### Video Content
**Intro (2 min):** Explain keyword scoring methodology  
**Code-Along (15 min):** Implement scoring algorithms, show calculations  
**Test (6 min):** Analyze real keyword, show scoring breakdown  
**Recap (2 min):** "Next: video pattern analysis"

### Common Challenges
- Algorithm tuning (getting scores to feel right)
- Handling edge cases (no data, very old videos)
- Caching strategy and invalidation

---

## Episode 5: Video Analysis Service
**Month:** 5  
**Time Estimate:** 22 hours (slightly heavier—more patterns)  
**Complexity:** Medium (statistical analysis)

### What Gets Built
- VideoAnalyzer service for pattern detection
- Statistical analysis (mean, median, std dev)
- Title, tag, duration correlation analysis
- Engagement rate calculations
- Most common tag clustering
- Pattern detection ("successful videos have X characteristics")
- CSV/JSON export of analyzed videos
- REST endpoint for video analysis

### Key Technical Concepts
- Statistical descriptive analysis
- Correlation calculations
- Data aggregation and grouping
- Export formatters (CSV, JSON)
- Pattern discovery algorithms

### Code Deliverables
```csharp
public class VideoAnalysisService
{
    public async Task<VideoAnalysisResult> AnalyzeNicheAsync(
        string keyword,
        int sampleSize = 20)
    {
        var videos = await _youtubeClient.SearchAsync(keyword, sampleSize);
        
        var analysis = new VideoAnalysisResult
        {
            Keyword = keyword,
            SampleSize = videos.Count,
            
            // Title statistics
            AvgTitleLength = Math.Round(videos.Average(v => v.Title.Length), 2),
            MinTitleLength = videos.Min(v => v.Title.Length),
            MaxTitleLength = videos.Max(v => v.Title.Length),
            TitleLengthStdDev = CalculateStdDev(videos.Select(v => (double)v.Title.Length)),
            
            // Duration statistics
            AvgDuration = Math.Round(videos.Average(v => v.DurationSeconds), 0),
            
            // Tag statistics
            AvgTagCount = Math.Round(videos.Average(v => v.Tags?.Count ?? 0), 2),
            
            // Engagement statistics
            AvgEngagementRate = Math.Round(
                videos.Average(v => (v.LikeCount + v.CommentCount) / (double)(v.ViewCount + 1) * 100), 2),
            
            // Most common tags
            MostCommonTags = videos
                .SelectMany(v => v.Tags ?? new List<string>())
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new TagCount { Tag = g.Key, Count = g.Count() })
                .ToList(),
            
            AnalyzedAt = DateTime.UtcNow
        };
        
        // Find patterns
        analysis.Patterns = ExtractPatterns(videos, analysis);
        
        return analysis;
    }
    
    private List<string> ExtractPatterns(
        List<Video> videos,
        VideoAnalysisResult analysis)
    {
        var patterns = new List<string>();
        
        if (analysis.AvgTitleLength > 50)
            patterns.Add($"Successful titles average {analysis.AvgTitleLength} characters (longer titles)");
        
        if (analysis.AvgDuration < 600)
            patterns.Add($"Most successful videos are under 10 minutes ({analysis.AvgDuration} seconds avg)");
        
        if (analysis.AvgTagCount >= 10)
            patterns.Add($"Videos use {analysis.AvgTagCount} tags on average");
        
        if (analysis.MostCommonTags.Any())
            patterns.Add($"Most common tag: '{analysis.MostCommonTags.First().Tag}' (appears in {analysis.MostCommonTags.First().Count} videos)");
        
        return patterns;
    }
    
    private double CalculateStdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        var avg = list.Average();
        var variance = list.Average(x => Math.Pow(x - avg, 2));
        return Math.Sqrt(variance);
    }
}

public class VideoAnalysisResult
{
    public string Keyword { get; set; }
    public int SampleSize { get; set; }
    
    // Title stats
    public double AvgTitleLength { get; set; }
    public int MinTitleLength { get; set; }
    public int MaxTitleLength { get; set; }
    public double TitleLengthStdDev { get; set; }
    
    // Duration
    public double AvgDuration { get; set; }
    
    // Tags
    public double AvgTagCount { get; set; }
    public List<TagCount> MostCommonTags { get; set; }
    
    // Engagement
    public double AvgEngagementRate { get; set; }
    
    // Discovered patterns
    public List<string> Patterns { get; set; }
    
    public DateTime AnalyzedAt { get; set; }
}
```

### Export Functionality
```csharp
public class VideoExporter
{
    public string ExportToCsv(List<Video> videos)
    {
        var csv = "Title,Channel,Duration,Views,Likes,Comments,EngagementRate,UploadDate\n";
        
        foreach (var video in videos)
        {
            var engagementRate = (video.LikeCount + video.CommentCount) / 
                (double)(video.ViewCount + 1) * 100;
                
            csv += $"\"{video.Title}\",\"{video.ChannelName}\",{video.DurationSeconds},{video.ViewCount},{video.LikeCount},{video.CommentCount},{engagementRate:F2},{video.UploadDate:yyyy-MM-dd}\n";
        }
        
        return csv;
    }
}
```

### Testing Checklist
- [ ] Analyze 20 videos → returns statistical summary
- [ ] Title length stats are reasonable (5-255 chars)
- [ ] Tag count reflects actual data
- [ ] Engagement rate calculation is correct
- [ ] Standard deviation calculated properly
- [ ] Most common tags identified
- [ ] Patterns extracted match data
- [ ] Export to CSV/JSON works
- [ ] Results persist in database

### Video Content
**Intro (2 min):** Show video pattern examples, explain why this matters  
**Code-Along (16 min):** Implement statistics, show pattern extraction  
**Test (5 min):** Analyze videos, show patterns, export CSV  
**Recap (2 min):** "Next: trend detection"

### Common Challenges
- Handling videos with missing data
- Statistical calculations (standard deviation, correlation)
- CSV escaping and formatting

---

## Episode 6: Trend Detection Service
**Month:** 6  
**Time Estimate:** 20 hours (standard)  
**Complexity:** Medium (time-series logic)

### What Gets Built
- TrendDetection service with growth calculations
- Week-over-week, month-over-month comparisons
- Momentum scoring (balancing growth + volume)
- Trend lifecycle classification (emerging/growing/mature/declining)
- Historical data aggregation
- REST endpoint for trend analysis
- Database storage of trend data

### Key Technical Concepts
- Time-series data manipulation
- Period-over-period calculations
- Ranking algorithms (balancing multiple factors)
- Data aggregation patterns
- Lifecycle classification

### Code Deliverables
```csharp
public class TrendDetectionService
{
    public async Task<List<TrendingKeyword>> FindTrendingAsync(
        string niche,
        int daysBack = 30,
        int minVolume = 100)
    {
        var currentPeriod = await GetKeywordVolumesAsync(niche, days: 7);
        var previousPeriod = await GetKeywordVolumesAsync(niche, days: 14, daysBack: 7);
        
        var trends = new List<TrendingKeyword>();
        
        foreach (var keyword in currentPeriod)
        {
            var previous = previousPeriod.FirstOrDefault(k => 
                k.Keyword == keyword.Keyword);
                
            var previousVolume = previous?.Volume ?? 1;
            var currentVolume = keyword.Volume;
            
            // Calculate growth rate
            var growthRate = ((currentVolume - previousVolume) / 
                (double)previousVolume) * 100;
            
            // Calculate momentum (growth + log scale for volume)
            var momentum = growthRate * Math.Log(currentVolume + 1);
            
            trends.Add(new TrendingKeyword
            {
                Keyword = keyword.Keyword,
                CurrentVolume = currentVolume,
                PreviousVolume = previousVolume,
                GrowthRate = Math.Round(growthRate, 2),
                MomentumScore = Math.Round(momentum, 2),
                TrendLifecycle = ClassifyLifecycle(growthRate, momentum)
            });
        }
        
        // Filter and sort
        return trends
            .Where(t => t.CurrentVolume >= minVolume)
            .OrderByDescending(t => t.MomentumScore)
            .ToList();
    }
    
    private async Task<List<KeywordVolume>> GetKeywordVolumesAsync(
        string niche,
        int days = 7,
        int daysBack = 0)
    {
        var startDate = DateTime.UtcNow.AddDays(-(daysBack + days));
        var endDate = DateTime.UtcNow.AddDays(-daysBack);
        
        var keywords = await _db.Keywords
            .Where(k => k.Niche == niche && 
                   k.LastAnalyzed >= startDate &&
                   k.LastAnalyzed <= endDate)
            .GroupBy(k => k.Keyword)
            .Select(g => new KeywordVolume
            {
                Keyword = g.Key,
                Volume = g.Sum(k => k.SearchVolumeRelative)
            })
            .ToListAsync();
            
        return keywords;
    }
    
    private string ClassifyLifecycle(double growthRate, double momentum)
    {
        if (growthRate > 50)
            return "emerging";
        if (growthRate > 20)
            return "growing";
        if (growthRate < -20)
            return "declining";
        return "mature";
    }
}

public class TrendingKeyword
{
    public string Keyword { get; set; }
    public int CurrentVolume { get; set; }
    public int PreviousVolume { get; set; }
    public double GrowthRate { get; set; }
    public double MomentumScore { get; set; }
    public string TrendLifecycle { get; set; }
}
```

### Testing Checklist
- [ ] Detect rising keywords over 7-day window
- [ ] Growth rate calculation is correct
- [ ] Momentum balances growth + absolute volume
- [ ] Lifecycle classification matches expectations
- [ ] Results sorted by momentum
- [ ] Min volume filter works
- [ ] Historical data aggregation correct
- [ ] Different time windows produce different results

### Video Content
**Intro (2 min):** Explain trend detection importance, show examples  
**Code-Along (15 min):** Implement growth calculations, momentum scoring  
**Test (6 min):** Detect rising keywords, show momentum ranking  
**Recap (2 min):** "Phase 2 done! Now: LLM integration"

### Common Challenges
- Handling missing data for previous periods
- Momentum formula tuning (growth vs. volume balance)
- Lifecycle classification edge cases

---

---

## PHASE 3: LLM INTEGRATION (Months 7-9)

---

## Episode 7: LLM Abstraction & Multi-Provider Setup
**Month:** 7  
**Time Estimate:** 25 hours (heavier—new complexity)  
**Complexity:** High (multiple API integrations)

### What Gets Built
- ILlmProvider abstraction layer
- OpenAI implementation (GPT-4o, GPT-4o-mini)
- Anthropic implementation (Claude 3.5 Sonnet)
- Groq implementation (Mixtral, for speed + cost)
- LlmOrchestrator for smart provider selection
- Cost tracking for every LLM call
- Prompt template system
- Error handling and fallback logic
- Configuration system for provider selection

### Key Technical Concepts
- Provider abstraction patterns
- Multiple API integrations (OpenAI, Anthropic, Groq)
- Cost tracking and attribution
- Prompt engineering patterns
- Error handling and fallbacks
- Configuration management

### Code Deliverables
```csharp
// ILlmProvider.cs interface
public interface ILlmProvider
{
    string ProviderName { get; }
    Task<LlmResponse> GenerateAsync(
        string prompt,
        LlmOptions options = null,
        CancellationToken ct = default);
}

public class LlmResponse
{
    public string Content { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public decimal CostUsd { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class LlmOptions
{
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
    public string Model { get; set; }
}

// OpenAiProvider.cs
public class OpenAiProvider : ILlmProvider
{
    public string ProviderName => "OpenAI";
    
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public async Task<LlmResponse> GenerateAsync(
        string prompt,
        LlmOptions options = null,
        CancellationToken ct = default)
    {
        var model = options?.Model ?? "gpt-4o";
        var request = new
        {
            model = model,
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = options?.MaxTokens ?? 1000,
            temperature = options?.Temperature ?? 0.7
        };
        
        var jsonContent = new StringContent(
            JsonConvert.SerializeObject(request),
            Encoding.UTF8,
            "application/json");
            
        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            jsonContent,
            ct);
            
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonConvert.DeserializeObject<OpenAiResponse>(json);
        
        var choice = result.Choices[0];
        return new LlmResponse
        {
            Content = choice.Message.Content,
            PromptTokens = result.Usage.PromptTokens,
            CompletionTokens = result.Usage.CompletionTokens,
            CostUsd = CalculateCost(model, result.Usage.PromptTokens, result.Usage.CompletionTokens),
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    private decimal CalculateCost(string model, int promptTokens, int completionTokens)
    {
        return model switch
        {
            "gpt-4o" => (promptTokens * 0.000005m) + (completionTokens * 0.000015m),
            "gpt-4o-mini" => (promptTokens * 0.00000015m) + (completionTokens * 0.0000006m),
            _ => 0m
        };
    }
}

// AnthropicProvider.cs
public class AnthropicProvider : ILlmProvider
{
    public string ProviderName => "Anthropic";
    
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public async Task<LlmResponse> GenerateAsync(
        string prompt,
        LlmOptions options = null,
        CancellationToken ct = default)
    {
        var model = options?.Model ?? "claude-3-5-sonnet-20241022";
        var request = new
        {
            model = model,
            max_tokens = options?.MaxTokens ?? 1024,
            messages = new[] { new { role = "user", content = prompt } }
        };
        
        var jsonContent = new StringContent(
            JsonConvert.SerializeObject(request),
            Encoding.UTF8,
            "application/json");
            
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        
        var response = await _httpClient.PostAsync(
            "https://api.anthropic.com/v1/messages",
            jsonContent,
            ct);
            
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonConvert.DeserializeObject<AnthropicResponse>(json);
        
        return new LlmResponse
        {
            Content = result.Content[0].Text,
            PromptTokens = result.Usage.InputTokens,
            CompletionTokens = result.Usage.OutputTokens,
            CostUsd = CalculateCost(model, result.Usage.InputTokens, result.Usage.OutputTokens),
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    private decimal CalculateCost(string model, int inputTokens, int outputTokens)
    {
        return model switch
        {
            "claude-3-5-sonnet-20241022" => (inputTokens * 0.000003m) + (outputTokens * 0.000015m),
            _ => 0m
        };
    }
}

// GroqProvider.cs (fastest, cheapest)
public class GroqProvider : ILlmProvider
{
    public string ProviderName => "Groq";
    
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public async Task<LlmResponse> GenerateAsync(
        string prompt,
        LlmOptions options = null,
        CancellationToken ct = default)
    {
        var model = options?.Model ?? "mixtral-8x7b-32768";
        // Implementation similar to OpenAI
        // Groq is API-compatible with OpenAI
    }
}

// LlmOrchestrator.cs - Smart provider selection
public class LlmOrchestrator
{
    private readonly Dictionary<string, ILlmProvider> _providers;
    private readonly ILlmCostRepository _costRepo;
    private readonly IConfiguration _config;
    
    public LlmOrchestrator(
        IEnumerable<ILlmProvider> providers,
        ILlmCostRepository costRepo,
        IConfiguration config)
    {
        _providers = providers.ToDictionary(p => p.ProviderName);
        _costRepo = costRepo;
        _config = config;
    }
    
    public async Task<LlmResponse> ExecuteAsync(
        string taskType,
        string prompt,
        Guid userId,
        CancellationToken ct = default)
    {
        // Select best provider for task
        var providerName = SelectProvider(taskType);
        var provider = _providers[providerName];
        
        try
        {
            var response = await provider.GenerateAsync(prompt, ct: ct);
            
            // Track cost for billing
            await _costRepo.LogCostAsync(new LlmCost
            {
                UserId = userId,
                Provider = providerName,
                Model = provider.ProviderName,
                PromptTokens = response.PromptTokens,
                CompletionTokens = response.CompletionTokens,
                CostUsd = response.CostUsd,
                CreatedAt = DateTime.UtcNow
            });
            
            return response;
        }
        catch (RateLimitException)
        {
            // Fallback to next best provider
            return await ExecuteWithFallbackAsync(taskType, prompt, userId, providerName, ct);
        }
    }
    
    private string SelectProvider(string taskType)
    {
        return taskType switch
        {
            "KeywordInsights" => "Anthropic",      // Best for nuanced analysis
            "QuickSummary" => "Groq",              // Fast + cheap
            "StrategicAdvice" => "Anthropic",      // Deep thinking
            "TranscriptAnalysis" => "Anthropic",   // Good at long context
            _ => "OpenAI"
        };
    }
    
    private async Task<LlmResponse> ExecuteWithFallbackAsync(
        string taskType,
        string prompt,
        Guid userId,
        string failedProvider,
        CancellationToken ct)
    {
        // Try OpenAI as fallback
        if (failedProvider != "OpenAI")
        {
            return await _providers["OpenAI"].GenerateAsync(prompt, ct: ct);
        }
        
        throw new LlmException("All LLM providers failed");
    }
}
```

### Configuration
```json
{
  "Llm": {
    "Providers": {
      "OpenAi": {
        "ApiKey": "sk-...",
        "BaseUrl": "https://api.openai.com/v1"
      },
      "Anthropic": {
        "ApiKey": "sk-ant-...",
        "BaseUrl": "https://api.anthropic.com/v1"
      },
      "Groq": {
        "ApiKey": "gsk-...",
        "BaseUrl": "https://api.groq.com/openai/v1"
      }
    },
    "TaskProviderMapping": {
      "KeywordInsights": "Anthropic",
      "QuickSummary": "Groq",
      "StrategicAdvice": "Anthropic",
      "TranscriptAnalysis": "Anthropic"
    }
  }
}
```

### Testing Checklist
- [ ] Call all 3 providers successfully
- [ ] Cost tracking accurate for each provider
- [ ] Tokens counted correctly
- [ ] Provider selection uses config
- [ ] Fallback works if provider rate-limited
- [ ] Responses cached and reused
- [ ] Cost comparison between providers visible

### Video Content
**Intro (3 min):** Why multi-provider matters, show cost/quality trade-offs  
**Code-Along (17 min):** Build provider implementations, orchestrator  
**Test (4 min):** Call all 3 providers, show cost comparison  
**Recap (2 min):** "Next: integrating LLM into analyses"

### Common Challenges
- API authentication differences between providers
- Token counting accuracy
- Cost calculation precision
- Handling provider-specific response formats

---

## Episode 8: Add LLM to Keyword Analysis
**Month:** 8  
**Time Estimate:** 20 hours (standard)  
**Complexity:** Medium (service composition)

### What Gets Built
- Enhance KeywordAnalyzer with LLM insights
- Conditional execution (Free vs. Pro tier)
- Usage metering (track LLM calls per user)
- Tier enforcement in service layer
- Storage of LLM costs linked to analysis
- REST endpoint with `includeInsights` parameter
- UI preparation for insights display

### Key Technical Concepts
- Service composition (combining services)
- Conditional feature execution
- Usage metering and quota tracking
- Tier-based access control
- Cost attribution per feature

### Code Deliverables
```csharp
public class EnhancedKeywordAnalyzerService
{
    private readonly KeywordAnalysisService _baseAnalyzer;
    private readonly ILlmService _llmService;
    private readonly PlanEnforcer _planEnforcer;
    private readonly UsageMeter _usageMeter;
    
    public async Task<KeywordAnalysisResult> AnalyzeAsync(
        Guid userId,
        string keyword,
        string niche = null,
        bool includeInsights = false)
    {
        // Always do base analysis
        var baseAnalysis = await _baseAnalyzer.AnalyzeAsync(keyword, niche);
        
        // Only LLM if requested and user's tier allows
        if (includeInsights)
        {
            // Check plan allows this feature
            await _planEnforcer.EnforceFeatureAccessAsync(
                userId, 
                "llm_insights");
            
            // Generate insights via LLM
            var insightsPrompt = PromptTemplates.KeywordInsights(baseAnalysis);
            var insights = await _llmService.ExecuteAsync(
                "KeywordInsights",
                insightsPrompt,
                userId);
            
            // Track usage for this user
            await _usageMeter.RecordAsync(
                userId,
                "llm_call",
                insights.CompletionTokens);
            
            return new KeywordAnalysisResult
            {
                Keyword = keyword,
                Niche = niche,
                SearchVolume = baseAnalysis.SearchVolume,
                CompetitionScore = baseAnalysis.CompetitionScore,
                TrendDirection = baseAnalysis.TrendDirection,
                KeywordIntent = baseAnalysis.KeywordIntent,
                LlmInsights = insights.Content,
                LlmEnhanced = true,
                AnalyzedAt = DateTime.UtcNow
            };
        }
        
        return new KeywordAnalysisResult
        {
            Keyword = keyword,
            Niche = niche,
            SearchVolume = baseAnalysis.SearchVolume,
            CompetitionScore = baseAnalysis.CompetitionScore,
            TrendDirection = baseAnalysis.TrendDirection,
            KeywordIntent = baseAnalysis.KeywordIntent,
            LlmEnhanced = false,
            AnalyzedAt = DateTime.UtcNow
        };
    }
}

// PlanEnforcer.cs
public class PlanEnforcer
{
    private readonly IUserRepository _userRepo;
    
    public async Task EnforceFeatureAccessAsync(
        Guid userId,
        string feature)
    {
        var user = await _userRepo.GetAsync(userId);
        
        var allowedFeatures = user.SubscriptionTier switch
        {
            "free" => new[] { "keyword_research", "video_metadata" },
            "pro" => new[] { "keyword_research", "video_metadata", "transcripts", "llm_insights", "niche_comparison" },
            "enterprise" => new[] { "*" },
            _ => Array.Empty<string>()
        };
        
        if (!allowedFeatures.Contains(feature) && !allowedFeatures.Contains("*"))
            throw new FeatureAccessDeniedException(
                $"Feature '{feature}' not available in {user.SubscriptionTier} tier. Upgrade to Pro.");
    }
}

// UsageMeter.cs
public class UsageMeter
{
    private readonly IUsageLogRepository _usageLogRepo;
    
    public async Task RecordAsync(Guid userId, string feature, int units)
    {
        var log = new UsageLog
        {
            UserId = userId,
            Feature = feature,
            UnitCount = units,
            BillingPeriod = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        };
        
        await _usageLogRepo.AddAsync(log);
    }
}

// Controller
[HttpPost("keywords")]
public async Task<IActionResult> AnalyzeKeyword(
    [FromBody] KeywordAnalysisRequest request)
{
    var userId = User.FindFirst("sub")?.Value;
    
    var result = await _keywordService.AnalyzeAsync(
        new Guid(userId),
        request.Keyword,
        request.Niche,
        includeInsights: request.IncludeInsights);
        
    return Ok(result);
}
```

### PromptTemplates
```csharp
public static class PromptTemplates
{
    public static string KeywordInsights(KeywordAnalysis analysis) =>
        $@"You are a YouTube strategy expert. Analyze this keyword research data and provide strategic insights:

Keyword: {analysis.Keyword}
Niche: {analysis.Niche}
Search Volume: {analysis.SearchVolume}/100
Competition Score: {analysis.CompetitionScore}/100
Trend: {analysis.TrendDirection}
Intent: {analysis.KeywordIntent}

Provide:
1. Why creators should care about this keyword
2. Specific positioning strategies to stand out
3. Content gaps you could exploit
4. 3 unique video angle ideas that haven't been overdone

Be specific and actionable. Assume the creator is intermediate level (not beginner, not expert).";
}
```

### Testing Checklist
- [ ] Free tier: can analyze but no insights
- [ ] Pro tier: gets insights with LLM call
- [ ] LLM call costs tracked accurately
- [ ] Usage meter increments correctly
- [ ] Insights are relevant and actionable
- [ ] Tier enforcement blocks appropriately
- [ ] Response time reasonable (1-3 seconds)

### Video Content
**Intro (2 min):** Show what insights look like, explain tier differences  
**Code-Along (14 min):** Add LLM to analyzer, implement tier checks  
**Test (6 min):** Show Free tier (no insights), Pro tier (with insights)  
**Recap (2 min):** "Next: transcript analysis"

### Common Challenges
- Prompt engineering (getting useful insights)
- Tier enforcement consistency
- Usage tracking accuracy

---

## Episode 9: YouTube Transcripts + LLM Analysis
**Month:** 9  
**Time Estimate:** 22 hours (slightly heavier—integration)  
**Complexity:** Medium (multi-service orchestration)

### What Gets Built
- Transcript fetching from YouTube API
- Transcript caching in PostgreSQL (never refetch)
- LLM analysis of transcript content
- Enhanced video analyzer using transcripts
- Content keyword extraction from transcripts
- Key takeaways summarization
- Engagement moment identification
- REST endpoint for transcript analysis

### Key Technical Concepts
- Long-context LLM prompts (handling 5-30K tokens)
- Token budgeting (fitting transcript into prompt)
- Summarization patterns
- Keyword extraction from content
- Content analysis techniques

### Code Deliverables
```csharp
public class TranscriptAnalysisService
{
    private readonly IYouTubeClient _youtubeClient;
    private readonly ILlmService _llmService;
    private readonly ITranscriptRepository _transcriptRepo;
    
    public async Task<TranscriptAnalysisResult> AnalyzeAsync(
        Guid userId,
        string videoId,
        bool includeInsights = false)
    {
        // Fetch or retrieve cached transcript
        var transcript = await _youtubeClient.GetTranscriptAsync(videoId);
        
        // Basic analysis: structure, metrics
        var baseAnalysis = new TranscriptAnalysisResult
        {
            VideoId = videoId,
            TranscriptLength = transcript.Length,
            WordCount = transcript.Split(" ").Length,
            SentenceCount = transcript.Split(".").Length,
            TopicsIdentified = ExtractTopics(transcript)
        };
        
        // LLM enhancements (if tier allows)
        if (includeInsights)
        {
            // Create truncated prompt if transcript is very long
            var truncatedTranscript = TruncateForTokenLimit(transcript, maxTokens: 8000);
            
            var insights = await _llmService.ExecuteAsync(
                "TranscriptAnalysis",
                PromptTemplates.TranscriptAnalysis(truncatedTranscript),
                userId);
            
            baseAnalysis.LlmAnalysis = insights.Content;
            baseAnalysis.LlmEnhanced = true;
        }
        
        return baseAnalysis;
    }
    
    private List<string> ExtractTopics(string transcript)
    {
        // Simple: extract longest sentences (likely topic statements)
        var sentences = transcript.Split(new[] { ".", "?", "!" }, StringSplitOptions.RemoveEmptyEntries);
        
        return sentences
            .Where(s => s.Length > 30)
            .OrderByDescending(s => s.Length)
            .Take(10)
            .Select(s => s.Trim())
            .ToList();
    }
    
    private string TruncateForTokenLimit(string transcript, int maxTokens)
    {
        // Rough estimate: 1 token ≈ 4 characters
        var maxChars = maxTokens * 4;
        
        if (transcript.Length <= maxChars)
            return transcript;
            
        // Truncate and add ellipsis
        return transcript.Substring(0, maxChars) + "...[truncated]";
    }
}

public class TranscriptAnalysisResult
{
    public string VideoId { get; set; }
    public int TranscriptLength { get; set; }
    public int WordCount { get; set; }
    public int SentenceCount { get; set; }
    public List<string> TopicsIdentified { get; set; }
    
    // LLM-generated analysis
    public string LlmAnalysis { get; set; }
    public bool LlmEnhanced { get; set; }
}

// Enhanced VideoAnalyzer using transcripts
public class EnhancedVideoAnalyzer
{
    public async Task<DetailedVideoAnalysis> AnalyzeWithTranscriptsAsync(
        Guid userId,
        string keyword,
        int sampleSize = 20,
        bool includeInsights = false)
    {
        var videos = await _youtubeClient.SearchAsync(keyword, sampleSize);
        
        var analysis = new DetailedVideoAnalysis
        {
            Keyword = keyword,
            SampleSize = videos.Count,
            // ... standard video metrics ...
        };
        
        // Fetch transcripts for each video
        foreach (var video in videos)
        {
            try
            {
                var transcript = await _youtubeClient.GetTranscriptAsync(video.YoutubeId);
                video.TranscriptText = transcript;
                
                // Extract keywords from transcripts
                var keywords = ExtractKeywordsFromTranscript(transcript);
                analysis.TranscriptKeywords.AddRange(keywords);
            }
            catch
            {
                // Video might not have captions
            }
        }
        
        // Aggregate transcript insights
        analysis.MostCommonTranscriptTopics = analysis.TranscriptKeywords
            .GroupBy(k => k)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();
        
        return analysis;
    }
    
    private List<string> ExtractKeywordsFromTranscript(string transcript)
    {
        // Simple keyword extraction (could be improved with NLP)
        var words = transcript.Split(" ")
            .Where(w => w.Length > 5)  // Filter short words
            .Select(w => w.ToLower())
            .Distinct()
            .Take(20)
            .ToList();
            
        return words;
    }
}
```

### Prompt Template for Transcript Analysis
```csharp
public static string TranscriptAnalysis(string transcript) =>
    $@"Analyze this YouTube video transcript and provide content insights:

TRANSCRIPT (first {transcript.Length/1000}K chars shown):
{transcript}

Identify and explain:
1. **Main Topics**: What are the 3-5 core topics covered?
2. **Key Takeaways**: What are the most valuable insights for viewers?
3. **Engagement Hooks**: What moments or statements would make viewers want to click/watch?
4. **Missing Elements**: What related topics could have been covered but weren't?
5. **Content Angle**: What makes this video's approach unique compared to typical videos on this topic?
6. **Call-to-Action Effectiveness**: How does the creator wrap up? Is there a CTA?

Format as actionable insights for a creator planning similar content.";
```

### Testing Checklist
- [ ] Fetch transcript for known video with captions
- [ ] Transcript cached, second fetch is instant
- [ ] Base analysis: word count, sentence count accurate
- [ ] Topic extraction identifies main subjects
- [ ] LLM insights relevant to content
- [ ] Long transcripts truncated properly
- [ ] Keyword extraction from transcript works
- [ ] Results persist in database

### Video Content
**Intro (2 min):** Show why transcript analysis is powerful  
**Code-Along (16 min):** Fetch transcripts, implement analysis, LLM integration  
**Test (5 min):** Analyze real video transcript, show insights  
**Recap (2 min):** "LLM phase complete! Next: plugins & frontend"

### Common Challenges
- Handling videos without transcripts
- Long transcript token management
- Keyword extraction quality (might use more sophisticated NLP later)

---

---

## PHASE 4: PLUGINS & FRONTEND (Months 10-12)

---

## Episode 10: Plugin Architecture & Niche Comparison
**Month:** 10  
**Time Estimate:** 25 hours (heavier—system design)  
**Complexity:** High (new subsystem)

### What Gets Built
- Plugin discovery system (from /plugins directory)
- Plugin manifest validation (plugin.json)
- Plugin loading and execution framework
- NicheComparison service (orchestrates plugins)
- Sample plugin: TextSearchVolumePlugin
- Plugin registry in PostgreSQL
- REST endpoints for plugin management
- Admin endpoints for enable/disable plugins

### Key Technical Concepts
- Extensible architecture design
- Reflection and dynamic type loading
- Plugin lifecycle management
- Service abstraction for plugins
- Configuration-driven extensibility
- Cross-plugin communication

### Code Deliverables (see VARA_Plugin_LLM_Architecture.md)

### Testing Checklist
- [ ] Discover plugins from /plugins directory
- [ ] Plugin manifest validates
- [ ] Plugin loads and executes
- [ ] Results returned in correct format
- [ ] NicheComparison orchestrates plugins
- [ ] Gap analysis identifies opportunities
- [ ] New plugin loadable without restart
- [ ] Plugins can be enabled/disabled

### Video Content
**Intro (3 min):** Why plugin architecture matters for community, show examples  
**Code-Along (17 min):** Implement plugin discovery, execution framework, sample plugin  
**Test (4 min):** Load plugin, run niche comparison using plugins  
**Recap (2 min):** "Next: frontend and real-time dashboard"

### Common Challenges
- Plugin isolation (preventing malicious code)
- Plugin versioning and compatibility
- Cross-plugin dependency management

---

## Episode 11: Frontend Setup & Real-Time Dashboard
**Month:** 11  
**Time Estimate:** 28 hours (heaviest—new framework)  
**Complexity:** High (full UI implementation)

### What Gets Built
- SvelteKit project scaffolding
- Authentication flow (JWT in localStorage)
- Dashboard layout
- SignalR connection for real-time progress
- Analysis trigger UI (forms for each analysis type)
- Results display components
- Usage/tier display component
- Error handling and notifications
- Responsive design

### Key Technical Concepts
- Svelte component design
- SvelteKit routing and layout
- SignalR client-side integration
- Form validation and submission
- Real-time UI updates
- State management (Svelte stores)

### Code Deliverables (Example Svelte components)
```svelte
<!-- Dashboard.svelte -->
<script>
  import { onMount } from 'svelte';
  import { HubConnectionBuilder } from '@microsoft/signalr';
  import AnalysisProgress from './AnalysisProgress.svelte';
  import ResultsDisplay from './ResultsDisplay.svelte';
  
  let keyword = '';
  let analyzing = false;
  let progress = 0;
  let results = null;
  let error = null;
  
  let connection;
  
  onMount(async () => {
    connection = new HubConnectionBuilder()
      .withUrl('https://localhost:5000/api/analysis')
      .withAutomaticReconnect()
      .build();
      
    connection.on('AnalysisProgress', (data) => {
      progress = data.percent;
    });
    
    connection.on('AnalysisComplete', (data) => {
      results = data;
      analyzing = false;
    });
    
    connection.on('AnalysisError', (error) => {
      error = error.message;
      analyzing = false;
    });
    
    await connection.start();
  });
  
  async function analyzeKeyword() {
    error = null;
    analyzing = true;
    progress = 0;
    results = null;
    
    try {
      await connection.invoke('StartAnalysis', {
        type: 'keyword',
        keyword: keyword,
        includeInsights: true
      });
    } catch (e) {
      error = e.message;
      analyzing = false;
    }
  }
</script>

<div class="container">
  <h1>Keyword Analysis</h1>
  
  <form on:submit|preventDefault={analyzeKeyword}>
    <input
      bind:value={keyword}
      placeholder="Enter keyword..."
      disabled={analyzing}
      required
    />
    <button type="submit" disabled={analyzing || !keyword}>
      {analyzing ? 'Analyzing...' : 'Analyze'}
    </button>
  </form>
  
  {#if analyzing}
    <AnalysisProgress percent={progress} />
  {/if}
  
  {#if results}
    <ResultsDisplay {results} />
  {/if}
  
  {#if error}
    <div class="error-box">{error}</div>
  {/if}
</div>

<style>
  .container {
    max-width: 800px;
    margin: 0 auto;
    padding: 20px;
  }
  
  form {
    display: flex;
    gap: 10px;
    margin: 20px 0;
  }
  
  input {
    flex: 1;
    padding: 10px;
    border: 1px solid #ccc;
    border-radius: 4px;
    font-size: 16px;
  }
  
  button {
    padding: 10px 20px;
    background-color: #007bff;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 16px;
  }
  
  button:disabled {
    background-color: #ccc;
    cursor: not-allowed;
  }
  
  .error-box {
    background-color: #f8d7da;
    color: #721c24;
    padding: 12px;
    border-radius: 4px;
    margin-top: 20px;
  }
</style>
```

### Testing Checklist
- [ ] Login works, redirects to dashboard
- [ ] Can trigger keyword analysis
- [ ] Progress bar updates in real-time
- [ ] Results display correctly
- [ ] Tier info shows at top
- [ ] Navigation between analysis types works
- [ ] Error messages are clear
- [ ] Responsive on mobile
- [ ] SignalR connection stays alive

### Video Content
**Intro (3 min):** Show what the dashboard looks like, explain real-time aspect  
**Code-Along (18 min):** Build auth flow, dashboard layout, SignalR integration  
**Test (5 min):** Run full flow: login → trigger analysis → see real-time progress  
**Recap (2 min):** "Almost done! Final: polish and launch"

### Common Challenges
- SignalR connection debugging
- Real-time state management
- Responsive design complexity
- Browser storage (JWT token management)

---

## Episode 12: Polish, Optimization & Release
**Month:** 12  
**Time Estimate:** 20 hours (standard—bug fixes, docs, launch)  
**Complexity:** Medium (various small improvements)

### What Gets Built
- Performance optimization (slow queries, N+1)
- Comprehensive error handling
- Improved logging and observability
- API documentation (Swagger complete)
- Database seed data (for testing)
- Docker multi-stage build optimization
- CI/CD pipeline (GitHub Actions)
- README and deployment guide
- Open-source licensing
- GitHub release and changelog

### Key Areas
- Database query optimization and indexing
- Response caching strategies
- Error boundary components
- Production-grade logging (structured logs)
- Health checks and liveness probes
- Graceful degradation
- Documentation completeness

### Deliverables
```
✓ API responds in <500ms (cached queries)
✓ All database queries properly indexed
✓ Comprehensive error messages
✓ Swagger docs 100% complete
✓ Docker compose runs with single command
✓ Can deploy to Hetzner with Terraform
✓ README explains all features and setup
✓ Open-sourced on GitHub with MIT license
✓ First release tagged v1.0.0
```

### Production Checklist
- [ ] All endpoints documented in Swagger
- [ ] Error responses standardized
- [ ] Logging structured and queryable
- [ ] Database indexes on all filter columns
- [ ] API latency measured and optimized
- [ ] Frontend bundled and minified
- [ ] Docker image optimized (multi-stage)
- [ ] CI/CD pipeline automated
- [ ] README complete with setup instructions
- [ ] CONTRIBUTING.md for community
- [ ] LICENSE file (MIT recommended)
- [ ] First release created on GitHub
- [ ] Deploy to Hetzner successful
- [ ] Health check endpoint functional

### Video Content
**Intro (2 min):** Recap 12-month journey, show what was built  
**Code-Along (12 min):** Quick optimization pass, show deployment  
**Test (4 min):** Full workflow from login to saved analysis  
**Recap (4 min):** Summary, thank community, link to GitHub, what's next  
**Total: ~22 minutes**

---

---

## BONUS EPISODES

---

## Bonus Episode A: Performance Optimization
**Time Estimate:** 15 hours  
**Release Timing:** Month 11-12 if ahead of schedule

### Topics
- Batch processing for multiple videos
- Query optimization (N+1 fixes)
- Index analysis and creation
- Caching strategy refinement
- API response compression
- Frontend lazy loading
- Asset optimization

### Deliverable
```
✓ Analyze 50 videos in <2 minutes
✓ Repeat analysis cache-hit in <100ms
✓ Database queries < 100ms
✓ Frontend loads in <3 seconds
```

---

## Bonus Episode B: Mock Billing UI
**Time Estimate:** 18 hours  
**Release Timing:** Month 12

### Topics
- Tier selection UI on frontend
- Usage display (X analyses remaining)
- Mock upgrade buttons (Stripe placeholder)
- Pricing table display
- Usage history/charts
- Cost breakdown by LLM provider
- Quota warning system

### Deliverable
```
✓ Show "5/100 analyses used this month"
✓ Upgrade button functional (redirect to Stripe later)
✓ Cost breakdown visible
✓ Usage chart by analysis type
✓ Quota warnings before limit
```

---

## Bonus Episode C: Deployment & Self-Hosting Guide
**Time Estimate:** 20 hours  
**Release Timing:** Post-launch

### Topics
- Kubernetes manifests (for cloud deployment)
- Environment configuration guide
- Database backup/restore guide
- Monitoring setup (logs, metrics)
- CI/CD deepdive (GitHub Actions)
- Community contribution guide
- Plugin development guide

### Deliverable
```
✓ Deploy to any cloud (AWS, Azure, GCP)
✓ Self-host on own VPS
✓ Automated backups configured
✓ Monitoring dashboard setup
✓ Community can submit PRs
```

---

## Summary Table

| Episode | Month | Phase | Focus | Status | Time |
|---------|-------|-------|-------|--------|------|
| 1 | 1 | Foundation | Auth + Database | ⏳ 25h |
| 2 | 2 | Foundation | YouTube API | ⏳ 20h |
| 3 | 3 | Foundation | REST API | ⏳ 18h |
| 4 | 4 | Analysis | Keyword Research | ⏳ 20h |
| 5 | 5 | Analysis | Video Analysis | ⏳ 22h |
| 6 | 6 | Analysis | Trend Detection | ⏳ 20h |
| 7 | 7 | LLM | Multi-Provider | ⏳ 25h |
| 8 | 8 | LLM | LLM + Keywords | ⏳ 20h |
| 9 | 9 | LLM | Transcripts | ⏳ 22h |
| 10 | 10 | Advanced | Plugins | ⏳ 25h |
| 11 | 11 | Frontend | Dashboard | ⏳ 28h |
| 12 | 12 | Polish | Launch | ⏳ 20h |
| **Total** | | | **Core MVP** | | **245h** |
| A | 11-12 | Bonus | Performance | Optional | 15h |
| B | 12 | Bonus | Billing UI | Optional | 18h |
| C | Post | Bonus | Deployment | Optional | 20h |

---

**You've got 12 months. Go build something great.**
