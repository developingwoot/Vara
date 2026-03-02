using Microsoft.EntityFrameworkCore;
using Vara.Api.Models.Entities;

namespace Vara.Api.Data;

public static class SeedData
{
    public static async Task SeedInitialKeywordsAsync(VaraContext db)
    {
        if (await db.SeedKeywords.AnyAsync()) return;

        db.SeedKeywords.AddRange(BuildKeywords());
        await db.SaveChangesAsync();
    }

    private static IEnumerable<SeedKeyword> BuildKeywords()
    {
        // ---------------------------------------------------------------
        // Web Dev — 50 keywords
        // ---------------------------------------------------------------
        var webDev = new (string kw, string cat, int pri)[]
        {
            // Foundational
            ("react tutorial",              "foundational", 1),
            ("javascript beginner",         "foundational", 2),
            ("html css basics",             "foundational", 3),
            ("python web development",      "foundational", 4),
            ("node.js tutorial",            "foundational", 5),
            ("vue.js tutorial",             "foundational", 6),
            ("angular tutorial",            "foundational", 7),
            ("typescript beginner",         "foundational", 8),
            ("git for beginners",           "foundational", 9),
            ("rest api tutorial",           "foundational", 10),
            ("docker tutorial",             "foundational", 11),
            ("linux for developers",        "foundational", 12),
            ("sql tutorial",                "foundational", 13),
            ("mongodb tutorial",            "foundational", 14),
            ("graphql tutorial",            "foundational", 15),
            ("postgresql tutorial",         "foundational", 16),
            ("redis tutorial",              "foundational", 17),
            ("kubernetes tutorial",         "foundational", 18),
            ("nginx tutorial",              "foundational", 19),
            ("web development roadmap",     "foundational", 20),
            // Popular
            ("next.js tutorial",            "popular", 21),
            ("tailwind css",                "popular", 22),
            ("svelte tutorial",             "popular", 23),
            ("bun javascript",              "popular", 24),
            ("astro framework tutorial",    "popular", 25),
            ("remix tutorial",              "popular", 26),
            ("vite tutorial",               "popular", 27),
            ("serverless tutorial",         "popular", 28),
            ("vercel deployment",           "popular", 29),
            ("cloudflare workers",          "popular", 30),
            ("react hooks tutorial",        "popular", 31),
            ("zustand tutorial",            "popular", 32),
            ("react query tutorial",        "popular", 33),
            ("prisma tutorial",             "popular", 34),
            ("drizzle orm tutorial",        "popular", 35),
            ("trpc tutorial",               "popular", 36),
            ("pnpm tutorial",               "popular", 37),
            ("deno tutorial",               "popular", 38),
            ("aws lambda tutorial",         "popular", 39),
            ("edge functions tutorial",     "popular", 40),
            // Emerging
            ("web assembly 2025",           "emerging", 41),
            ("htmx tutorial",               "emerging", 42),
            ("server components react",     "emerging", 43),
            ("ai coding tools",             "emerging", 44),
            ("cursor ide tutorial",         "emerging", 45),
            ("github copilot tips",         "emerging", 46),
            ("claude code tutorial",        "emerging", 47),
            ("v0 dev tutorial",             "emerging", 48),
            ("shadcn ui tutorial",          "emerging", 49),
            ("biome javascript",            "emerging", 50),
        };

        foreach (var (kw, cat, pri) in webDev)
            yield return new SeedKeyword { Keyword = kw, Niche = "Web Dev", Category = cat, Priority = pri };

        // ---------------------------------------------------------------
        // Content Creation — 50 keywords
        // ---------------------------------------------------------------
        var content = new (string kw, string cat, int pri)[]
        {
            // Foundational
            ("youtube growth tips",             "foundational", 1),
            ("how to start youtube channel",    "foundational", 2),
            ("video editing beginner",          "foundational", 3),
            ("thumbnail design tips",           "foundational", 4),
            ("youtube algorithm explained",     "foundational", 5),
            ("content calendar planning",       "foundational", 6),
            ("youtube seo tips",                "foundational", 7),
            ("video script writing",            "foundational", 8),
            ("how to gain subscribers",         "foundational", 9),
            ("youtube channel audit",           "foundational", 10),
            ("youtube analytics explained",     "foundational", 11),
            ("how to make money youtube",       "foundational", 12),
            ("youtube niche ideas",             "foundational", 13),
            ("consistency on youtube",          "foundational", 14),
            ("youtube titles tips",             "foundational", 15),
            ("youtube description tips",        "foundational", 16),
            ("youtube tags tips",               "foundational", 17),
            ("batch filming videos",            "foundational", 18),
            ("repurpose content tips",          "foundational", 19),
            ("youtube vs tiktok",               "foundational", 20),
            // Popular
            ("video editing premiere pro",      "popular", 21),
            ("davinci resolve tutorial",        "popular", 22),
            ("capcut tutorial",                 "popular", 23),
            ("obs streaming tutorial",          "popular", 24),
            ("faceless youtube channel",        "popular", 25),
            ("youtube shorts strategy",         "popular", 26),
            ("youtube monetization tips",       "popular", 27),
            ("canva thumbnail tutorial",        "popular", 28),
            ("microphone for youtube",          "popular", 29),
            ("camera for youtube beginners",    "popular", 30),
            ("lighting setup youtube",          "popular", 31),
            ("screen recording tutorial",       "popular", 32),
            ("b roll footage tips",             "popular", 33),
            ("color grading tutorial",          "popular", 34),
            ("audio editing tips youtube",      "popular", 35),
            ("voiceover tips youtube",          "popular", 36),
            ("youtube automation channel",      "popular", 37),
            ("affiliate marketing youtube",     "popular", 38),
            ("sponsorship tips youtube",        "popular", 39),
            ("youtube community post tips",     "popular", 40),
            // Emerging
            ("ai video generation",             "emerging", 41),
            ("runway ml tutorial",              "emerging", 42),
            ("kling ai video tutorial",         "emerging", 43),
            ("ai thumbnail generator",          "emerging", 44),
            ("notebooklm podcast tutorial",     "emerging", 45),
            ("ai voiceover tools",              "emerging", 46),
            ("sora video generation",           "emerging", 47),
            ("invideo ai tutorial",             "emerging", 48),
            ("youtube ai tools 2025",           "emerging", 49),
            ("faceless ai youtube channel",     "emerging", 50),
        };

        foreach (var (kw, cat, pri) in content)
            yield return new SeedKeyword { Keyword = kw, Niche = "Content Creation", Category = cat, Priority = pri };

        // ---------------------------------------------------------------
        // 3D Printing — 50 keywords
        // ---------------------------------------------------------------
        var printing = new (string kw, string cat, int pri)[]
        {
            // Foundational
            ("3d printing beginner guide",      "foundational", 1),
            ("fdm vs resin printing",           "foundational", 2),
            ("3d printer settings guide",       "foundational", 3),
            ("how to slice 3d models",          "foundational", 4),
            ("3d printing filament guide",      "foundational", 5),
            ("3d printer calibration",          "foundational", 6),
            ("support settings 3d print",       "foundational", 7),
            ("first layer adhesion tips",       "foundational", 8),
            ("3d printer troubleshooting",      "foundational", 9),
            ("3d modeling beginner",            "foundational", 10),
            ("fusion 360 tutorial",             "foundational", 11),
            ("blender for 3d printing",         "foundational", 12),
            ("tinkercad tutorial",              "foundational", 13),
            ("how to design 3d prints",         "foundational", 14),
            ("3d print finishing techniques",   "foundational", 15),
            ("sanding 3d prints",               "foundational", 16),
            ("painting 3d prints",              "foundational", 17),
            ("3d printing tolerances guide",    "foundational", 18),
            ("infill patterns explained",       "foundational", 19),
            ("layer height settings 3d print",  "foundational", 20),
            // Popular
            ("bambu lab review",                "popular", 21),
            ("bambu lab a1 mini review",        "popular", 22),
            ("orca slicer tutorial",            "popular", 23),
            ("bambu lab x1 carbon review",      "popular", 24),
            ("prusa mk4 review",                "popular", 25),
            ("prusa xl review",                 "popular", 26),
            ("voron build guide",               "popular", 27),
            ("resin printing tips",             "popular", 28),
            ("elegoo saturn review",            "popular", 29),
            ("anycubic photon review",          "popular", 30),
            ("bambu ams tutorial",              "popular", 31),
            ("water washable resin review",     "popular", 32),
            ("abs vs pla filament",             "popular", 33),
            ("petg filament tips",              "popular", 34),
            ("tpu flexible filament guide",     "popular", 35),
            ("carbon fiber filament review",    "popular", 36),
            ("silk filament tips",              "popular", 37),
            ("3d printer enclosure diy",        "popular", 38),
            ("multi color 3d printing",         "popular", 39),
            ("klipper firmware tutorial",       "popular", 40),
            // Emerging
            ("bambu lab h2d review",            "emerging", 41),
            ("3d printing with ai",             "emerging", 42),
            ("generative design 3d print",      "emerging", 43),
            ("ai slicer tools 3d print",        "emerging", 44),
            ("bambu lab p1s review",            "emerging", 45),
            ("ams lite tutorial",               "emerging", 46),
            ("3d printing business ideas",      "emerging", 47),
            ("on demand 3d printing service",   "emerging", 48),
            ("sustainable 3d printing",         "emerging", 49),
            ("3d printing large format",        "emerging", 50),
        };

        foreach (var (kw, cat, pri) in printing)
            yield return new SeedKeyword { Keyword = kw, Niche = "3D Printing", Category = cat, Priority = pri };

        // ---------------------------------------------------------------
        // AI/ML — 50 keywords
        // ---------------------------------------------------------------
        var aiml = new (string kw, string cat, int pri)[]
        {
            // Foundational
            ("machine learning beginner",       "foundational", 1),
            ("neural networks explained",       "foundational", 2),
            ("python for ai",                   "foundational", 3),
            ("deep learning tutorial",          "foundational", 4),
            ("data science roadmap",            "foundational", 5),
            ("tensorflow tutorial",             "foundational", 6),
            ("pytorch tutorial",                "foundational", 7),
            ("scikit learn tutorial",           "foundational", 8),
            ("natural language processing",     "foundational", 9),
            ("computer vision tutorial",        "foundational", 10),
            ("reinforcement learning basics",   "foundational", 11),
            ("statistics for machine learning", "foundational", 12),
            ("linear regression explained",     "foundational", 13),
            ("decision trees explained",        "foundational", 14),
            ("random forest tutorial",          "foundational", 15),
            ("gradient boosting explained",     "foundational", 16),
            ("clustering algorithms tutorial",  "foundational", 17),
            ("pca dimensionality reduction",    "foundational", 18),
            ("model evaluation metrics ml",     "foundational", 19),
            ("train test split explained",      "foundational", 20),
            // Popular
            ("chatgpt tutorial 2025",           "popular", 21),
            ("prompt engineering guide",        "popular", 22),
            ("stable diffusion tutorial",       "popular", 23),
            ("midjourney tutorial",             "popular", 24),
            ("langchain tutorial",              "popular", 25),
            ("fine tuning llm guide",           "popular", 26),
            ("rag tutorial langchain",          "popular", 27),
            ("vector database tutorial",        "popular", 28),
            ("llm from scratch tutorial",       "popular", 29),
            ("hugging face tutorial",           "popular", 30),
            ("openai api tutorial",             "popular", 31),
            ("anthropic api tutorial",          "popular", 32),
            ("embeddings tutorial ml",          "popular", 33),
            ("semantic search tutorial",        "popular", 34),
            ("ai agent tutorial",               "popular", 35),
            ("llm evaluation guide",            "popular", 36),
            ("ai model deployment tutorial",    "popular", 37),
            ("mlops tutorial",                  "popular", 38),
            ("data pipeline tutorial",          "popular", 39),
            ("feature engineering tips ml",     "popular", 40),
            // Emerging
            ("local llm tutorial",              "emerging", 41),
            ("ollama tutorial",                 "emerging", 42),
            ("llama 3 tutorial",                "emerging", 43),
            ("mistral tutorial",                "emerging", 44),
            ("claude api tutorial",             "emerging", 45),
            ("open source ai 2025",             "emerging", 46),
            ("crew ai tutorial",                "emerging", 47),
            ("autogen tutorial",                "emerging", 48),
            ("computer use ai tutorial",        "emerging", 49),
            ("reasoning models explained",      "emerging", 50),
        };

        foreach (var (kw, cat, pri) in aiml)
            yield return new SeedKeyword { Keyword = kw, Niche = "AI/ML", Category = cat, Priority = pri };
    }
}
