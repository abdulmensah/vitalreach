using Microsoft.EntityFrameworkCore;

namespace VitalReach.Web.Data;

public static class CatalogSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        await EnsureProductColumnsAsync(db);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AdminUsers" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AdminUsers" PRIMARY KEY AUTOINCREMENT,
                "Email" TEXT NOT NULL,
                "NormalizedEmail" TEXT NOT NULL,
                "DisplayName" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "CreatedUtc" TEXT NOT NULL,
                "CreatedBy" TEXT NOT NULL,
                "UpdatedUtc" TEXT NOT NULL,
                "UpdatedBy" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AdminUsers_NormalizedEmail" ON "AdminUsers" ("NormalizedEmail");
            CREATE TABLE IF NOT EXISTS "Headquarters" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Headquarters" PRIMARY KEY,
                "CenterName" TEXT NOT NULL,
                "AddressLine1" TEXT NOT NULL,
                "AddressLine2" TEXT NOT NULL,
                "City" TEXT NOT NULL,
                "Region" TEXT NOT NULL,
                "PostalCode" TEXT NOT NULL,
                "Country" TEXT NOT NULL,
                "Phone" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "Hours" TEXT NOT NULL,
                "UpdatedUtc" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "ContactSubmissions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ContactSubmissions" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "Phone" TEXT NOT NULL,
                "Message" TEXT NOT NULL,
                "IsRead" INTEGER NOT NULL,
                "CreatedUtc" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "ProductImages" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ProductImages" PRIMARY KEY AUTOINCREMENT,
                "ProductId" INTEGER NOT NULL,
                "ImageUrl" TEXT NOT NULL,
                "AltText" TEXT NOT NULL,
                "SortOrder" INTEGER NOT NULL,
                CONSTRAINT "FK_ProductImages_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProductImages_ProductId_ImageUrl" ON "ProductImages" ("ProductId", "ImageUrl");
            """);
        if (!await db.Products.AnyAsync()) db.Products.AddRange(
            New("energy", "Ultra Energy Shot™", 24m, "Founder's Collection", "Caffeine-free focus & vitality", "30 servings", "gold-product", "30 mL", "Ultra Energy", "Shot", 10),
            New("magnesium", "Magnesium Glycinate Complex+", 29m, "Daily Wellness", "Everyday calm & muscle support", "60 capsules", "teal-product", "COMPLEX+", "Magnesium", "Complex+", 20),
            New("collagen", "Marine Collagen Glow", 38m, "Women's Beauty", "Beauty, hydration & glow support", "30 servings", "pearl-product", "GLOW", "Marine Collagen", "Glow", 30),
            New("beetroot", "Beetroot Plus", 27m, "Active Living", "Circulation, endurance & stamina", "60 capsules", "berry-product", "500 mg", "Beetroot", "Plus", 40));
        await AddMissingSnackProductsAsync(db);
        await AddMissingSupplementProductsAsync(db);
        await AddMissingPersonalCareProductsAsync(db);
        await AddMissingPackagingConceptProductsAsync(db);
        await AddMissingShelfCollectionProductsAsync(db);
        if (!await db.AdminUsers.AnyAsync()) db.AdminUsers.AddRange(
            AdminUser.Create("abdulmensah@gmail.com", "Abdul Mensah", "system-seed"),
            AdminUser.Create("masaoudaa@gmail.com", "Masaouda", "system-seed"));
        if (!await db.Headquarters.AnyAsync()) db.Headquarters.Add(new HeadquartersSettings());
        await db.SaveChangesAsync();
        await AddMissingProductGalleryImagesAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureProductColumnsAsync(CatalogDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            await using var columns = connection.CreateCommand();
            columns.CommandText = "PRAGMA table_info(\"Products\");";
            await using var reader = await columns.ExecuteReaderAsync();
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync())
                existingColumns.Add(reader.GetString(1));

            await reader.DisposeAsync();
            if (!existingColumns.Contains(nameof(ProductEntity.ImageUrl)))
                await AddColumnAsync(connection, "ALTER TABLE \"Products\" ADD COLUMN \"ImageUrl\" TEXT NULL;");
            if (!existingColumns.Contains(nameof(ProductEntity.Description)))
                await AddColumnAsync(connection, "ALTER TABLE \"Products\" ADD COLUMN \"Description\" TEXT NOT NULL DEFAULT '';");
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task AddColumnAsync(System.Data.Common.DbConnection connection, string sql)
    {
        await using var alter = connection.CreateCommand();
        alter.CommandText = sql;
        await alter.ExecuteNonQueryAsync();
    }

    private static async Task AddMissingSnackProductsAsync(CatalogDbContext db)
    {
        var snacks = new[]
        {
            Snack(
                "crunchy-soy-bites-sea-salt-ginger",
                "Crunchy Soy Bites – Sea Salt & Ginger",
                "A crisp, savory soybean snack with warming ginger and a clean touch of sea salt.",
                "Oven-roasted plant protein with a bright, balanced finish—made for convenient everyday snacking.",
                "/images/products/crunchy-soy-bites-sea-salt-ginger.jpg",
                "SEA SALT",
                "Sea Salt &",
                "Ginger",
                "teal-product",
                50),
            Snack(
                "crunchy-soy-bites-spicy-suya",
                "Crunchy Soy Bites – Spicy Suya",
                "Oven-roasted soy bites seasoned with a bold, warming West African suya-inspired spice blend.",
                "A crunchy source of plant protein with rich roasted flavor and a lively chili finish.",
                "/images/products/crunchy-soy-bites-spicy-suya.jpg",
                "SPICY",
                "Spicy Suya",
                "Soy Crunch",
                "berry-product",
                60),
            Snack(
                "crunchy-soy-bites-honey-ginger",
                "Crunchy Soy Bites – Honey Ginger",
                "Golden oven-roasted soy bites pairing gentle honey sweetness with the warmth of ginger.",
                "A satisfyingly crunchy plant-protein snack with a lightly sweet, naturally uplifting flavor.",
                "/images/products/crunchy-soy-bites-honey-ginger.jpg",
                "HONEY",
                "Honey Ginger",
                "Soy Crunch",
                "gold-product",
                70)
        };

        var slugs = snacks.Select(product => product.Slug).ToArray();
        var existingSlugs = await db.Products
            .Where(product => slugs.Contains(product.Slug))
            .Select(product => product.Slug)
            .ToListAsync();
        db.Products.AddRange(snacks.Where(product => !existingSlugs.Contains(product.Slug, StringComparer.OrdinalIgnoreCase)));
    }

    private static ProductEntity Snack(string slug, string name, string benefit, string description, string imageUrl, string orb, string one, string two, string theme, int order) => new()
    {
        Slug = slug,
        Name = name,
        Price = 8m,
        Category = "Healthy Snacks",
        Benefit = benefit,
        Description = description,
        Detail = "50 g pouch",
        Theme = theme,
        Orb = orb,
        LabelOne = one,
        LabelTwo = two,
        ImageUrl = imageUrl,
        IsPublished = true,
        SortOrder = order
    };

    private static async Task AddMissingSupplementProductsAsync(CatalogDbContext db)
    {
        var supplements = new[]
        {
            Supplement(
                "liposomal-glutathione-gold",
                "Liposomal Glutathione Gold",
                42m,
                "Daily Wellness",
                "Advanced antioxidant and radiance support",
                "A premium liposomal glutathione formula designed to complement everyday antioxidant, cellular, and skin-wellness routines.",
                "/images/products/liposomal-glutathione-gold.jpg",
                "GOLD",
                "Liposomal",
                "Glutathione Gold",
                "gold-product",
                80),
            Supplement(
                "collagen-glow",
                "Collagen Glow",
                38m,
                "Women's Beauty",
                "Beauty support for skin, hair, nails, and joints",
                "A premium collagen-focused beauty formula created to support a consistent skin, hair, nail, and active-living wellness routine.",
                "/images/products/collagen-glow.jpg",
                "GLOW",
                "Collagen",
                "Glow",
                "pearl-product",
                90),
            Supplement(
                "immune-shield",
                "Immune Shield",
                29m,
                "Daily Wellness",
                "Everyday immune and wellness support",
                "A daily protection formula developed to complement balanced nutrition and an everyday immune-wellness routine.",
                "/images/products/immune-shield.jpg",
                "SHIELD",
                "Immune",
                "Shield",
                "teal-product",
                100),
            Supplement(
                "b12-energy-film",
                "B12 Energy Film",
                22m,
                "Active Living",
                "Fast, convenient energy and focus support",
                "A convenient vitamin B12 formula for people looking to complement their energy, focus, and metabolism-support routine.",
                "/images/products/b12-energy-film.jpg",
                "B12",
                "B12 Energy",
                "Film",
                "gold-product",
                110),
            Supplement(
                "magnesium-balance",
                "Magnesium Balance",
                29m,
                "Daily Wellness",
                "Muscle, nerve, relaxation, and sleep support",
                "A balanced magnesium formula intended to complement evening relaxation and everyday muscle and nerve wellness.",
                "/images/products/magnesium-balance.jpg",
                "BALANCE",
                "Magnesium",
                "Balance",
                "berry-product",
                120),
            Supplement(
                "moringa-gold",
                "Moringa Gold",
                27m,
                "Daily Wellness",
                "Plant-based superfood nutrition for everyday vitality",
                "A moringa-based superfood supplement developed to complement daily nutrition, energy, and whole-body wellness routines.",
                "/images/products/moringa-gold.jpg",
                "MORINGA",
                "Moringa",
                "Gold",
                "teal-product",
                130),
            Supplement(
                "mens-performance-plus",
                "Men's Performance+",
                34m,
                "Men's Wellness",
                "Stamina, strength, and vitality support",
                "A men's wellness formula created to complement an active routine focused on stamina, strength, and everyday vitality.",
                "/images/products/mens-performance-plus.jpg",
                "VITALITY",
                "Men's",
                "Performance+",
                "teal-product",
                140),
            Supplement(
                "womens-harmony-plus",
                "Women's Harmony+",
                34m,
                "Women's Wellness",
                "Hormone, energy, mood, and wellness support",
                "A women's wellness formula designed to complement routines centered on balance, energy, mood, and whole-body wellbeing.",
                "/images/products/womens-harmony-plus.jpg",
                "HARMONY",
                "Women's",
                "Harmony+",
                "berry-product",
                150)
        };

        var slugs = supplements.Select(product => product.Slug).ToArray();
        var existingSlugs = await db.Products
            .Where(product => slugs.Contains(product.Slug))
            .Select(product => product.Slug)
            .ToListAsync();
        db.Products.AddRange(supplements.Where(product => !existingSlugs.Contains(product.Slug, StringComparer.OrdinalIgnoreCase)));
    }

    private static ProductEntity Supplement(string slug, string name, decimal price, string category, string benefit, string description, string imageUrl, string orb, string one, string two, string theme, int order) => new()
    {
        Slug = slug,
        Name = name,
        Price = price,
        Category = category,
        Benefit = benefit,
        Description = description,
        Detail = "60 capsules",
        Theme = theme,
        Orb = orb,
        LabelOne = one,
        LabelTwo = two,
        ImageUrl = imageUrl,
        IsPublished = true,
        SortOrder = order
    };

    private static async Task AddMissingPersonalCareProductsAsync(CatalogDbContext db)
    {
        var products = new[]
        {
            PersonalCare(
                "herbal-wellness-gift-set",
                "Herbal & Wellness Gift Set",
                72m,
                "Gift Sets",
                "A complete four-piece ritual for hair, face, and body",
                "A premium gift-ready collection featuring Jojoba Oil, Herbal Shampoo, Daily Glow Moisturizer, and Refreshing Body Wash in an elegant presentation box.",
                "4-piece gift set",
                "/images/products/herbal-wellness-gift-set.jpg",
                "GIFT SET",
                "Herbal & Wellness",
                "Gift Set",
                "gold-product",
                160),
            PersonalCare(
                "jojoba-oil",
                "Jojoba Oil",
                18m,
                "Natural Oils",
                "Cold-pressed moisture for skin and hair",
                "A pure, unrefined jojoba oil designed for versatile everyday care, helping nourish dry-feeling skin and condition hair.",
                "100 ml",
                "/images/products/jojoba-oil.jpg",
                "PURE",
                "Jojoba",
                "Oil",
                "gold-product",
                170),
            PersonalCare(
                "herbal-shampoo",
                "Herbal Shampoo",
                20m,
                "Hair Care",
                "A botanical cleanse for stronger-looking, healthy hair",
                "A gentle herbal shampoo combining jojoba oil, rosemary, and aloe vera for a refreshing cleanse suited to all hair types.",
                "400 ml",
                "/images/products/herbal-shampoo.jpg",
                "HERBAL",
                "Herbal",
                "Shampoo",
                "teal-product",
                180),
            PersonalCare(
                "daily-glow-moisturizer",
                "Daily Glow Moisturizer",
                24m,
                "Skin Care",
                "Daily hydration for soft, radiant-looking skin",
                "A lightweight daily moisturizer with jojoba oil, shea butter, and vitamin E, created for comfortable hydration across all skin types.",
                "100 ml",
                "/images/products/daily-glow-moisturizer.jpg",
                "GLOW",
                "Daily Glow",
                "Moisturizer",
                "pearl-product",
                190),
            PersonalCare(
                "refreshing-body-wash",
                "Refreshing Body Wash",
                22m,
                "Body Care",
                "A gentle, hydrating cleanse that leaves skin feeling fresh",
                "A refreshing body wash with jojoba oil, aloe vera, and vitamin E, formulated for a soft and comfortable after-cleanse feel.",
                "500 ml",
                "/images/products/refreshing-body-wash.jpg",
                "REFRESH",
                "Refreshing",
                "Body Wash",
                "teal-product",
                200)
        };

        var slugs = products.Select(product => product.Slug).ToArray();
        var existingSlugs = await db.Products
            .Where(product => slugs.Contains(product.Slug))
            .Select(product => product.Slug)
            .ToListAsync();
        db.Products.AddRange(products.Where(product => !existingSlugs.Contains(product.Slug, StringComparer.OrdinalIgnoreCase)));
    }

    private static ProductEntity PersonalCare(string slug, string name, decimal price, string category, string benefit, string description, string detail, string imageUrl, string orb, string one, string two, string theme, int order) => new()
    {
        Slug = slug,
        Name = name,
        Price = price,
        Category = category,
        Benefit = benefit,
        Description = description,
        Detail = detail,
        Theme = theme,
        Orb = orb,
        LabelOne = one,
        LabelTwo = two,
        ImageUrl = imageUrl,
        IsPublished = true,
        SortOrder = order
    };

    private static async Task AddMissingPackagingConceptProductsAsync(CatalogDbContext db)
    {
        var products = new[]
        {
            Concept("womens-balance", "Women's Balance", 32m, "Women's Wellness", "Everyday hormonal and whole-body wellness support", "A balanced women's wellness formula developed to complement routines focused on vitality and overall wellbeing.", "60 capsules", "/images/products/womens-balance.jpg", "BALANCE", "Women's", "Balance", "berry-product", 210),
            Concept("womens-energy-shot", "Women's Energy Shot", 8m, "Energy Shots", "Convenient energy, vitality, and balance support", "A compact energy shot created for convenient use as part of an active women's wellness routine.", "60 ml", "/images/products/womens-energy-shot.jpg", "ENERGY", "Women's", "Energy Shot", "berry-product", 220),
            Concept("mens-energy-shot", "Men's Energy Shot", 8m, "Energy Shots", "Convenient energy, stamina, and performance support", "A compact energy shot created for convenient use alongside an active men's wellness routine.", "60 ml", "/images/products/mens-energy-shot.jpg", "ENERGY", "Men's", "Energy Shot", "teal-product", 230),
            Concept("castor-oil", "Castor Oil", 16m, "Premium Oils", "Cold-pressed care for skin and hair", "A pure cold-pressed castor oil suited to versatile skin, scalp, and hair-care routines.", "60 ml", "/images/products/castor-oil.jpg", "PURE", "Castor", "Oil", "gold-product", 240),
            Concept("black-seed-oil", "Black Seed Oil", 19m, "Premium Oils", "Pure cold-pressed botanical oil", "A cold-pressed black seed oil for customers building a simple, botanical self-care routine.", "60 ml", "/images/products/black-seed-oil.jpg", "PURE", "Black Seed", "Oil", "gold-product", 250),
            Concept("argan-oil", "Argan Oil", 22m, "Premium Oils", "Lightweight botanical moisture for skin and hair", "A pure cold-pressed argan oil designed to condition dry-feeling hair and nourish skin.", "60 ml", "/images/products/argan-oil.jpg", "PURE", "Argan", "Oil", "gold-product", 260),
            Concept("herbal-conditioner", "Herbal Conditioner", 20m, "Hair Care", "Moisture and repair support for healthy-looking hair", "A rich herbal conditioner created to complement cleansing and leave hair feeling soft and manageable.", "350 ml", "/images/products/herbal-conditioner.jpg", "HERBAL", "Herbal", "Conditioner", "teal-product", 270),
            Concept("hair-growth-serum", "Hair Growth Serum", 24m, "Hair Care", "Targeted nourishment for stronger-looking hair", "A lightweight scalp and hair serum developed to support a consistent nourishment and strengthening routine.", "100 ml", "/images/products/hair-growth-serum.jpg", "SERUM", "Hair Growth", "Serum", "teal-product", 280),
            Concept("premium-wellness-gift-box", "Premium Wellness Gift Box", 58m, "Gift Sets", "A coordinated three-piece wellness gift", "A premium presentation box containing Moringa Gold, Women's Balance, and Hair Growth Serum.", "3-piece gift set", "/images/products/premium-wellness-gift-box.jpg", "GIFT", "Premium Wellness", "Gift Box", "gold-product", 290)
        };

        var slugs = products.Select(product => product.Slug).ToArray();
        var existingSlugs = await db.Products.Where(product => slugs.Contains(product.Slug)).Select(product => product.Slug).ToListAsync();
        db.Products.AddRange(products.Where(product => !existingSlugs.Contains(product.Slug, StringComparer.OrdinalIgnoreCase)));
    }

    private static async Task AddMissingProductGalleryImagesAsync(CatalogDbContext db)
    {
        var gallery = new (string Slug, string Url, string Alt, int SortOrder)[]
        {
            ("moringa-gold", "/images/products/moringa-gold-concept.jpg", "Moringa Gold teal and gold packaging concept", 10),
            ("magnesium-balance", "/images/products/magnesium-balance-concept.jpg", "Magnesium Balance teal packaging concept", 10),
            ("immune-shield", "/images/products/immune-shield-concept.jpg", "Immune Shield teal and gold packaging concept", 10),
            ("beetroot", "/images/products/beetroot-plus-concept.jpg", "Beetroot Plus Men packaging concept", 10),
            ("energy", "/images/products/ultra-energy-shot-concept.jpg", "Ultra Energy Shot packaging concept", 10),
            ("jojoba-oil", "/images/products/jojoba-oil-concept.jpg", "Jojoba Oil teal packaging concept", 10),
            ("herbal-shampoo", "/images/products/herbal-shampoo-concept.jpg", "Herbal Shampoo teal packaging concept", 10),
            ("collagen-glow", "/images/products/collagen-glow-shelf.jpg", "Collagen Glow retail shelf display", 20),
            ("beetroot", "/images/products/beetroot-plus-shelf.jpg", "Beetroot Plus retail shelf display", 20),
            ("womens-balance", "/images/products/womens-balance-shelf.jpg", "Women's Balance retail shelf display", 20),
            ("moringa-gold", "/images/products/moringa-gold-shelf.jpg", "Moringa Gold retail shelf display", 20),
            ("liposomal-glutathione-gold", "/images/products/liposomal-glutathione-shelf.jpg", "Liposomal Glutathione retail shelf display", 20),
            ("prostate-support", "/images/products/prostate-support-shelf.jpg", "Prostate Support retail shelf display", 20),
            ("iron-folate", "/images/products/iron-folate-shelf.jpg", "Iron and Folate retail shelf display", 20),
            ("black-seed-oil", "/images/products/black-seed-oil-shelf.jpg", "Black Seed Oil retail shelf display", 20),
            ("immune-shield", "/images/products/immune-shield-shelf.jpg", "Immune Shield retail shelf display", 20),
            ("magnesium-balance", "/images/products/magnesium-balance-shelf.jpg", "Magnesium Balance retail shelf display", 20),
            ("prenatal-care", "/images/products/prenatal-care-shelf.jpg", "Prenatal Care retail shelf display", 20),
            ("castor-oil", "/images/products/castor-oil-shelf.jpg", "Castor Oil retail shelf display", 20),
            ("herbal-shampoo", "/images/products/herbal-shampoo-shelf.jpg", "Herbal Shampoo retail shelf display", 20),
            ("castor-oil-conditioner", "/images/products/castor-oil-conditioner-shelf.jpg", "Castor Oil Conditioner retail shelf display", 20),
            ("herbal-soap", "/images/products/herbal-soap-shelf.jpg", "Herbal Soap retail shelf display", 20)
        };

        var slugs = gallery.Select(image => image.Slug).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var products = await db.Products.Where(product => slugs.Contains(product.Slug)).ToDictionaryAsync(product => product.Slug, StringComparer.OrdinalIgnoreCase);
        var productIds = products.Values.Select(product => product.Id).ToArray();
        var existingUrls = await db.ProductImages.Where(image => productIds.Contains(image.ProductId)).Select(image => image.ImageUrl).ToListAsync();
        foreach (var image in gallery)
            if (products.TryGetValue(image.Slug, out var product) && !existingUrls.Contains(image.Url, StringComparer.OrdinalIgnoreCase))
                db.ProductImages.Add(new ProductImage { ProductId = product.Id, ImageUrl = image.Url, AltText = image.Alt, SortOrder = image.SortOrder });
    }

    private static async Task AddMissingShelfCollectionProductsAsync(CatalogDbContext db)
    {
        var products = new[]
        {
            Concept("prostate-support", "Prostate Support", 31m, "Men's Wellness", "Prostate, urinary, and everyday vitality support", "A men's wellness formula designed to complement a balanced routine focused on prostate and urinary wellness.", "60 capsules", "/images/products/prostate-support.jpg", "SUPPORT", "Prostate", "Support", "teal-product", 300),
            Concept("iron-folate", "Iron & Folate", 26m, "Women's Wellness", "Iron, folate, and everyday energy support", "A thoughtfully paired iron and folate supplement for everyday nutritional and blood-health support.", "60 capsules", "/images/products/iron-folate.jpg", "IRON", "Iron &", "Folate", "berry-product", 310),
            Concept("prenatal-care", "Prenatal Care", 32m, "Prenatal Wellness", "Everyday nutritional support for the prenatal journey", "A prenatal wellness formula created to complement clinician-guided nutrition before and during pregnancy.", "60 capsules", "/images/products/prenatal-care.jpg", "PRENATAL", "Prenatal", "Care", "pearl-product", 320),
            Concept("castor-oil-conditioner", "Castor Oil Conditioner", 20m, "Hair Care", "Moisture and softness for healthy-looking hair", "A conditioning formula featuring castor oil, created to leave hair feeling soft, manageable, and cared for.", "350 ml", "/images/products/castor-oil-conditioner.jpg", "REPAIR", "Castor Oil", "Conditioner", "teal-product", 330),
            Concept("herbal-soap", "Herbal Soap", 10m, "Body Care", "A gentle everyday cleanse with botanical appeal", "A botanical-inspired cleansing bar for a simple, refreshing everyday body-care routine.", "100 g bar", "/images/products/herbal-soap.jpg", "CLEANSE", "Herbal", "Soap", "gold-product", 340)
        };

        var slugs = products.Select(product => product.Slug).ToArray();
        var existingSlugs = await db.Products.Where(product => slugs.Contains(product.Slug)).Select(product => product.Slug).ToListAsync();
        db.Products.AddRange(products.Where(product => !existingSlugs.Contains(product.Slug, StringComparer.OrdinalIgnoreCase)));
    }

    private static ProductEntity Concept(string slug, string name, decimal price, string category, string benefit, string description, string detail, string imageUrl, string orb, string one, string two, string theme, int order) => new()
    {
        Slug = slug, Name = name, Price = price, Category = category, Benefit = benefit, Description = description,
        Detail = detail, ImageUrl = imageUrl, Orb = orb, LabelOne = one, LabelTwo = two, Theme = theme,
        IsPublished = true, SortOrder = order
    };

    private static ProductEntity New(string slug, string name, decimal price, string category, string benefit, string detail, string theme, string orb, string one, string two, int order) => new() { Slug=slug, Name=name, Price=price, Category=category, Benefit=benefit, Detail=detail, Theme=theme, Orb=orb, LabelOne=one, LabelTwo=two, SortOrder=order };
}
