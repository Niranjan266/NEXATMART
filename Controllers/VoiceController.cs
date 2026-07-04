using Microsoft.AspNetCore.Mvc;
using OnlineGroceryShop.Data;
using OnlineGroceryShop.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("Voice")]
public class VoiceController : Controller
{
    private readonly ApplicationDbContext _context;

    // ════════════════════════════════════════════════════════════════════════
    //  SYNONYM DICTIONARY  (Tamil romanized + English variants → canonical)
    //  Keys are matched WHOLE-WORD before fuzzy NLP, so even phonetically
    //  distant Tamil words resolve correctly without fuzzy guessing.
    // ════════════════════════════════════════════════════════════════════════
    private static readonly Dictionary<string, string> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Fruits ───────────────────────────────────────────────────────────
        ["appel"]="apple",       ["aple"]="apple",        ["applle"]="apple",
        ["apples"]="apple",      ["aapple"]="apple",      ["aepel"]="apple",
        ["vaazhai"]="banana",    ["vaazhai pazham"]="banana", ["vaalai pazham"]="banana",
        ["vaalai"]="banana",     ["vazhai"]="banana",     ["vazhaipazham"]="banana",
        ["bananas"]="banana",    ["bananaa"]="banana",
        ["maampazham"]="mango",  ["mampazham"]="mango",   ["maangai"]="mango",
        ["mangai"]="mango",      ["mangoes"]="mango",     ["mangos"]="mango",    ["aambai"]="mango",
        ["mathulai"]="pomegranate", ["madulai"]="pomegranate", ["mathalai"]="pomegranate",
        ["naarthai"]="orange",   ["nartha"]="orange",     ["narangi"]="orange",  ["oranges"]="orange",
        ["thiratchai"]="grape",  ["thiradchai"]="grape",  ["dratchai"]="grape",  ["grapes"]="grape",
        ["koyya"]="guava",       ["koyyapazham"]="guava", ["kovva"]="guava",     ["guavas"]="guava",
        ["pappali"]="papaya",    ["papali"]="papaya",     ["papayas"]="papaya",  ["pappaya"]="papaya",
        ["perikkai"]="pear",     ["peri kai"]="pear",     ["pears"]="pear",
        ["strawberries"]="strawberry", ["starberry"]="strawberry", ["strobery"]="strawberry",
        ["tharbusani"]="watermelon",   ["tharboosani"]="watermelon", ["tarboosani"]="watermelon",
        ["watermelons"]="watermelon",  ["water melon"]="watermelon",
        ["ananas"]="pineapple",  ["annanas"]="pineapple", ["annanasi"]="pineapple",
        ["pineapples"]="pineapple",    ["pine apple"]="pineapple",
        ["chikku"]="sapodilla",  ["chickku"]="sapodilla", ["sapota"]="sapodilla",
        ["elumichai"]="lemon",   ["elimichai"]="lemon",   ["nimbu"]="lemon",     ["lemons"]="lemon",
        ["cherries"]="cherry",   ["cheery"]="cherry",
        ["peach"]="peach",       ["peachs"]="peach",      ["peaches"]="peach",
        ["plum"]="plum",         ["plums"]="plum",        ["erik"]="plum",
        ["melon"]="melon",       ["melons"]="melon",

        // ── Vegetables ───────────────────────────────────────────────────────
        ["urulai kizhangu"]="potato", ["urulaikizhangu"]="potato", ["urulai"]="potato",
        ["urulai kilangu"]="potato",  ["potatoes"]="potato",       ["aloo"]="potato",
        ["thakkali"]="tomato",   ["takkali"]="tomato",    ["thamakali"]="tomato",
        ["tamato"]="tomato",     ["tamota"]="tomato",     ["tomatoes"]="tomato",  ["tumato"]="tomato",
        ["vengayam"]="onion",    ["venkayam"]="onion",    ["vengatam"]="onion",
        ["onions"]="onion",      ["pyaaz"]="onion",
        ["carrots"]="carrot",    ["gajar"]="carrot",      ["kaarot"]="carrot",    ["karots"]="carrot",
        ["keerai"]="spinach",    ["kerai"]="spinach",     ["keera"]="spinach",    ["pasalai"]="spinach",
        ["palak"]="spinach",     ["spinach"]="spinach",
        ["katharikkai"]="brinjal", ["kathiri kai"]="brinjal", ["kathiri"]="brinjal",
        ["katharikai"]="brinjal",  ["kadari"]="brinjal",
        ["eggplant"]="brinjal",  ["aubergine"]="brinjal", ["brinjals"]="brinjal", ["kathirikkai"]="brinjal",
        ["vendaikkai"]="okra",   ["vendai kai"]="okra",   ["vendai"]="okra",
        ["lady finger"]="okra",  ["ladyfinger"]="okra",   ["ladies finger"]="okra", ["bhindi"]="okra",
        ["kudamilagai"]="capsicum", ["kuda milagai"]="capsicum", ["kapsicom"]="capsicum",
        ["bell pepper"]="capsicum", ["bellpepper"]="capsicum",   ["capsicums"]="capsicum",
        ["green pepper"]="capsicum",["red pepper"]="capsicum",
        ["pookkosu"]="cauliflower", ["poo kosu"]="cauliflower",  ["phool gobi"]="cauliflower",
        ["gobi"]="cauliflower",  ["cauliflowers"]="cauliflower", ["color flower"]="cauliflower",
        ["pagarkkai"]="bitter gourd", ["pagar kai"]="bitter gourd", ["pagarkai"]="bitter gourd",
        ["karela"]="bitter gourd",    ["bitter gourd"]="bitter gourd",
        ["suraikkai"]="bottle gourd", ["surai kai"]="bottle gourd",  ["surai"]="bottle gourd",
        ["lauki"]="bottle gourd",     ["bottle gourd"]="bottle gourd",
        ["pattani"]="peas",      ["patani"]="peas",       ["payaru"]="peas",     ["green peas"]="peas",
        ["greenpeas"]="peas",    ["pea"]="peas",
        ["inji"]="ginger",       ["ingee"]="ginger",      ["adrak"]="ginger",    ["injee"]="ginger",
        ["poondu"]="garlic",     ["pundu"]="garlic",      ["pundi"]="garlic",    ["lahsun"]="garlic",
        ["manjal"]="turmeric",   ["manjall"]="turmeric",  ["haldi"]="turmeric",
        ["venthayam"]="fenugreek", ["vendayam"]="fenugreek", ["methi"]="fenugreek",
        ["kothamalli"]="coriander", ["kothimalli"]="coriander", ["kothamali"]="coriander",
        ["dhaniya"]="coriander", ["dhania"]="coriander",  ["cilantro"]="coriander",
        ["puthina"]="mint",      ["pudina"]="mint",       ["pudena"]="mint",     ["mintu"]="mint",
        ["minti"]="mint",
        ["kosu"]="cabbage",      ["muttaikkosu"]="cabbage", ["cabages"]="cabbage",["patta gobi"]="cabbage",
        ["beetroot"]="beetroot", ["beets"]="beetroot",    ["beet root"]="beetroot",
        ["vellarikkai"]="cucumber", ["kakdi"]="cucumber", ["cucumbers"]="cucumber",
        ["mushrooms"]="mushroom",["koon"]="mushroom",     ["dhingri"]="mushroom",
        ["mullangi"]="radish",   ["moolangi"]="radish",   ["mooli"]="radish",    ["radishes"]="radish",
        ["broccolis"]="broccoli",["brocoli"]="broccoli",  ["brokoli"]="broccoli",
        ["sweet corn"]="corn",   ["sweetcorn"]="corn",    ["makai"]="corn",      ["cholam"]="corn",

        // ── Dairy & Staples ───────────────────────────────────────────────────
        ["paal"]="milk",         ["pall"]="milk",         ["paall"]="milk",
        ["thayir"]="curd",       ["tayir"]="curd",        ["thaayir"]="curd",
        ["curd"]="yogurt",       ["yoghurt"]="yogurt",    ["dahi"]="yogurt",     ["dahee"]="yogurt",
        ["arisi"]="rice",        ["rices"]="rice",        ["basmati"]="rice",    ["arachi"]="rice",
        ["gothumai"]="wheat",    ["godumai"]="wheat",     ["gehun"]="wheat",
        ["maavu"]="flour",       ["mavu"]="flour",        ["atta"]="flour",      ["maida"]="flour",
        ["muttai"]="egg",        ["muttei"]="egg",        ["eggs"]="egg",        ["anda"]="egg",
        ["kozhi"]="chicken",     ["koli"]="chicken",      ["murgi"]="chicken",   ["chickens"]="chicken",
        ["paun"]="bread",        ["rotti"]="bread",       ["roti"]="bread",      ["breads"]="bread",
        ["brown bread"]="bread", ["white bread"]="bread",
        ["sakkarai"]="sugar",    ["sakkara"]="sugar",     ["chini"]="sugar",     ["shakkar"]="sugar",
        ["uppu"]="salt",         ["namak"]="salt",        ["salts"]="salt",
        ["nei"]="ghee",          ["ghee"]="ghee",
        ["vennai"]="butter",     ["makhan"]="butter",     ["batter"]="butter",
        ["paneer"]="paneer",     ["cottage cheese"]="paneer",
        ["kadala"]="chickpeas",  ["chana"]="chickpeas",   ["kadalai"]="chickpeas",["chole"]="chickpeas",
        ["ulundu"]="lentils",    ["paruppu"]="lentils",   ["dal"]="lentils",     ["dhal"]="lentils",
        ["toor dal"]="lentils",  ["moong dal"]="lentils",
        ["ennai"]="oil",         ["oils"]="oil",          ["sunflower oil"]="oil",["coconut oil"]="oil",
        ["coconut"]="coconut",   ["tengai"]="coconut",    ["thengai"]="coconut",
        ["tomato sauce"]="ketchup", ["tomato ketchup"]="ketchup",

        // ── Sweets & Desserts ─────────────────────────────────────────────────
        ["inippu"]="sweet",      ["inippugal"]="sweet",   ["mithai"]="sweet",
        ["alwa"]="halwa",        ["halwa"]="halwa",       ["aluva"]="halwa",
        ["payasam"]="pudding",   ["kheer"]="pudding",
        ["kesari"]="dessert",    ["sheera"]="dessert",
        ["laddu"]="laddoo",      ["ladu"]="laddoo",       ["ladoo"]="laddoo",
        ["burfi"]="barfi",       ["borfi"]="barfi",
        ["mysore pak"]="mysorepak", ["mysurpa"]="mysorepak",
    };

    // QUALITY/MODIFIER ADJECTIVES — stripped before fuzzy matching
    private static readonly string[] Modifiers =
    {
        "fresh","organic","natural","raw","ripe","unripe",
        "big","small","large","medium","nice","good","best",
        "quality","premium","healthy","clean","pure","new",
        "local","imported","seasonal","daily","regular","frozen",
        "dried","whole","half","sliced","chopped","ground",
    };

    // CATEGORY KEYWORDS (DB: 1=Vegetables 2=Fruits 3=Desserts)
    private static readonly (string keyword, int id)[] CategoryKeywords =
    {
        ("vegetables",1),("vegetable",1),("veggie",1),("veggies",1),("greens",1),("sabzi",1),("sabji",1),
        ("kaaikari",1),("kai kari",1),("keerai",1),
        ("fruits",2),("fruit",2),("fresh fruit",2),("fresh fruits",2),
        ("pazham",2),("pazhangal",2),
        ("desserts",3),("dessert",3),("sweets",3),("sweet",3),("cake",3),("pastry",3),("mithai",3),
        ("inippu",3),("inippugal",3),("payasam",3),("kesari",3),
    };

    // FILLER PHRASES — removed before matching (longest first)
    private static readonly string[] FillerPhrases =
    {
        "can i please have some","can i please have","i would like to order some","i would like to order",
        "i would like to buy some","i would like to buy","i want to order some","i want to order",
        "i want to buy some","i want to buy","i am looking for some","i am looking for",
        "i'm looking for some","i'm looking for","could you please get me","could you get me",
        "can i have some","can i have","can i get some","can i get",
        "i would like some","i would like","i'd like to buy some","i'd like to buy",
        "i'd like to order some","i'd like to order","i'd like some","i'd like",
        "i will take some","i will take","i'll take some","i'll take",
        "i want some","i want","i need to buy some","i need to buy","i need some","i need",
        "please get me some","please get me","get me some","get me",
        "give me some","give me","bring me some","bring me",
        "add to my cart","add to cart","add some","add",
        "find me some","find me","show me some","show me",
        "search for some","search for","look for some","look for",
        "do you sell some","do you sell","do you have some","do you have",
        "is there any","is there","let me get some","let me get","let me have some","let me have",
        "grab me some","grab me","how about some","how about",
        "what about some","what about","i'll have some","i'll have",
        "please","some","a few","a bit of","any","one","a","an","the",
    };

    public VoiceController(ApplicationDbContext context) => _context = context;

    [HttpPost("search")]
    public IActionResult Search([FromBody] VoiceRequest request)
    {
        var rawSpeech = request.Speech?.ToLower().Trim() ?? "";
        var alts = (request.Alternatives ?? new List<string>())
            .Select((a, i) => new {
                text = a.ToLower().Trim(),
                conf = (request.Confidences != null && i < request.Confidences.Count)
                       ? request.Confidences[i] : 1.0
            })
            .Where(x => x.text.Length > 0).ToList();

        // Candidates: primary speech first, then alternatives sorted by confidence
        var candidates = new List<string> { rawSpeech }
            .Concat(alts.OrderByDescending(a => a.conf).Select(a => a.text))
            .Distinct().ToList();

        // ── DELIVERY ─────────────────────────────────────────────────────────
        string[] deliveryKws = { "deliver","when will i get","how long","shipping","eta",
                                  "arrival","when will it arrive","when will my order","track" };
        if (candidates.Any(t => deliveryKws.Any(k => t.Contains(k))))
            return Json(new { found=false, isDeliveryInfo=true,
                message="Your order will be delivered within 30 to 45 minutes to your address. Thank you for shopping with Nexamart!" });

        // ── CHECKOUT ──────────────────────────────────────────────────────────
        string[] checkoutKws = { "check out","checkout","place order","confirm order","buy now",
                                   "proceed","pay now","make payment","complete purchase" };
        if (candidates.Any(t => checkoutKws.Any(k => t.Contains(k))))
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = cartJson == null ? new List<OrderDetail>()
                     : JsonSerializer.Deserialize<List<OrderDetail>>(cartJson) ?? new();
            if (!cart.Any())
                return Json(new { found=false, isCheckout=true,
                    message="Your cart is empty. Please add some items first." });
            var summary = string.Join(", ", cart.Select(i => $"{i.Quantity} {i.Product?.Name}"));
            return Json(new { found=true, isCheckout=true,
                message=$"You have {summary} in your cart. Shall I proceed to checkout?" });
        }

        var products = _context.Products.ToList();

        // ── CATEGORY BROWSING ─────────────────────────────────────────────────
        foreach (var text in candidates)
        {
            var cl = NormalizeFull(text);
            foreach (var (kw, catId) in CategoryKeywords.OrderByDescending(c => c.keyword.Length))
            {
                if (cl.Contains(kw))
                {
                    var cp = products.Where(p => p.CategoryId == catId && p.StockQuantity > 0)
                                     .OrderBy(p => p.Price).ToList();
                    if (!cp.Any())
                        return Json(new { found=false, isCategory=true, categoryId=catId,
                            message=$"Sorry, no {kw} in stock right now. Check back soon!" });
                    var nameList = string.Join(", ", cp.Take(5).Select(p => p.Name));
                    return Json(new { found=false, isCategory=true, categoryId=catId,
                        message=$"We have {cp.Count} {kw} available: {nameList}. Which one would you like?",
                        suggestions=cp.Take(6).Select(p => new { id=p.Id, name=p.Name, price=p.Price.ToString("F0") }) });
                }
            }
        }

        // ── PRICE / STOCK QUERIES ─────────────────────────────────────────────
        string[] cheapKws = { "cheapest","most affordable","lowest price","least expensive","cheap","budget" };
        string[] premKws  = { "most expensive","premium","highest price","best quality","luxury" };
        string[] stockKws = { "what do you have","what's available","what is available",
                               "show me everything","what's in stock","all products","show all" };
        foreach (var text in candidates)
        {
            if (cheapKws.Any(k => text.Contains(k))) {
                var p = products.Where(x => x.StockQuantity > 0).OrderBy(x => x.Price).FirstOrDefault();
                if (p != null) return FoundProduct(p);
            }
            if (premKws.Any(k => text.Contains(k))) {
                var p = products.Where(x => x.StockQuantity > 0).OrderByDescending(x => x.Price).FirstOrDefault();
                if (p != null) return FoundProduct(p);
            }
            if (stockKws.Any(k => text.Contains(k))) {
                var avail = products.Where(x => x.StockQuantity > 0).OrderBy(x => x.Name).Take(8).ToList();
                var names = string.Join(", ", avail.Select(p => p.Name));
                return Json(new { found=false, isCategory=true, categoryId=0,
                    message=$"We currently have: {names}. Which one would you like?",
                    suggestions=avail.Select(p => new { id=p.Id, name=p.Name, price=p.Price.ToString("F0") }) });
            }
        }

        // ── NLP PRODUCT SEARCH ────────────────────────────────────────────────
        // For each candidate: generate normalized form + synonym-expanded form,
        // then test ALL n-gram sub-windows (1–4 words) against every product.
        // This handles extra words, accidental fillers, and accent variations.
        Product? bestProduct = null;
        double bestScore = double.MaxValue;

        foreach (var text in candidates)
        {
            var normalized = NormalizeFull(text);
            var expanded   = ExpandSynonyms(normalized);

            foreach (var variant in new[] { normalized, expanded }.Distinct())
            {
                if (string.IsNullOrWhiteSpace(variant)) continue;

                // a) full variant phrase against all products
                var (pFull, sFull) = RankProducts(variant, products);
                if (pFull != null && sFull < bestScore) { bestScore = sFull; bestProduct = pFull; }

                // b) all n-gram sub-windows (handles "I want fresh tomato today" → "tomato")
                var words = variant.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int n = 1; n <= Math.Min(4, words.Length); n++)
                {
                    for (int i = 0; i <= words.Length - n; i++)
                    {
                        var gram = string.Join(" ", words.Skip(i).Take(n));
                        if (gram.Length < 2) continue;
                        var (pGram, sGram) = RankProducts(gram, products);
                        if (pGram != null && sGram < bestScore) { bestScore = sGram; bestProduct = pGram; }
                    }
                }
            }
        }

        if (bestProduct != null && bestScore < 0.32)
            return FoundProduct(bestProduct);

        // ── FUZZY SUGGESTIONS (score < 0.62) ─────────────────────────────────
        var cleanMain = ExpandSynonyms(NormalizeFull(rawSpeech));
        var top3 = products
            .Select(p => new { p, score = CompositeScore(cleanMain, p.Name.ToLower()) })
            .OrderBy(x => x.score).Take(3).Where(x => x.score < 0.62)
            .Select(x => (object)new { id=x.p.Id, name=x.p.Name, price=x.p.Price.ToString("F0") })
            .ToList();

        return Json(top3.Any()
            ? new { found=false, suggestions=top3 }
            : new { found=false, suggestions=new List<object>() });
    }

    // ════════════════════════════════════════════════════════════════════════
    //  NORMALIZATION PIPELINE
    // ════════════════════════════════════════════════════════════════════════

    // Full pipeline: filler removal → plural/suffix norm → modifier strip → clean
    private static string NormalizeFull(string text)
    {
        var s = text.ToLower();

        // 1. Strip filler phrases (longest first to avoid partial-removal)
        foreach (var phrase in FillerPhrases.OrderByDescending(p => p.Length))
            s = s.Replace(phrase, " ");

        // 2. Suffix/plural normalization
        s = Regex.Replace(s, @"\b(\w{4,})ies\b",  m => m.Groups[1].Value + "y"); // berries→berry
        s = Regex.Replace(s, @"\b(\w{4,})ves\b",  m => m.Groups[1].Value + "f"); // leaves→leaf
        s = Regex.Replace(s, @"\b(\w{4,})es\b",   "$1");                          // tomatoes→tomato
        s = Regex.Replace(s, @"\b(\w{3,})s\b",    "$1");                          // apples→apple

        // 3. Strip quality/modifier adjectives
        foreach (var mod in Modifiers)
            s = Regex.Replace(s, $@"\b{Regex.Escape(mod)}\b", " ", RegexOptions.IgnoreCase);

        // 4. Collapse whitespace
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    // Expand synonyms using whole-word boundary matching (prevents "pea" in "peach")
    private static string ExpandSynonyms(string text)
    {
        foreach (var kv in Synonyms.OrderByDescending(kv => kv.Key.Length))
        {
            var pattern = $@"(?<![a-zA-Z]){Regex.Escape(kv.Key)}(?![a-zA-Z])";
            text = Regex.Replace(text, pattern, kv.Value, RegexOptions.IgnoreCase);
        }
        return text.Trim();
    }

    // Find the best-matching product for a given speech string
    private static (Product? product, double score) RankProducts(string speech, List<Product> products)
    {
        Product? best = null; double bestScore = double.MaxValue;
        foreach (var p in products) {
            double s = CompositeScore(speech, p.Name.ToLower());
            if (s < bestScore) { bestScore = s; best = p; }
        }
        return (best, bestScore);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  COMPOSITE NLP SCORE  (lower = better; 0.0 = perfect match)
    //
    //  Signals combined:
    //    1. Exact / whole-word containment shortcuts
    //    2. Best word-pair score (each speech token vs each product token)
    //       → JaroWinkler + Levenshtein + Soundex
    //    3. N-gram sub-phrase sweep (speech sub-windows vs full product name)
    //    4. Phrase-level score (full speech vs full product name)
    //    5. Token coverage bonus (product tokens found in speech tokens)
    // ════════════════════════════════════════════════════════════════════════
    private static double CompositeScore(string speech, string productName)
    {
        if (string.IsNullOrWhiteSpace(speech)) return 1.0;
        speech      = speech.Trim();
        productName = productName.Trim();

        // Exact full match
        if (speech == productName) return 0.0;

        var sw = speech.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var pw = productName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // All product tokens appear as whole words in speech → excellent match
        if (pw.Length > 0 && pw.All(p => p.Length >= 2 && sw.Contains(p))) return 0.02;

        // Any product token is an exact speech token (length ≥ 3)
        if (pw.Any(p => p.Length >= 3 && sw.Contains(p))) return 0.05;

        // ── Best word-pair score ──────────────────────────────────────────────
        double bestWord = 1.0;
        foreach (var s in sw) {
            if (s.Length < 3) continue;
            foreach (var p in pw) {
                if (p.Length < 2) continue;
                double ws = WordScore(s, p);
                if (ws < bestWord) bestWord = ws;
            }
        }

        // ── N-gram sub-phrase sweep (speech windows vs full product name) ─────
        double bestNgram = 1.0;
        for (int n = 1; n <= Math.Min(3, sw.Length); n++) {
            for (int i = 0; i <= sw.Length - n; i++) {
                var gram = string.Join(" ", sw.Skip(i).Take(n));
                if (gram.Length < 2) continue;
                double jw  = 1.0 - JaroWinkler(gram, productName);
                double lev = (double)Levenshtein(gram, productName)
                             / Math.Max(gram.Length, productName.Length);
                double ns  = jw * 0.6 + lev * 0.4;
                if (ns < bestNgram) bestNgram = ns;
            }
        }

        // ── Phrase-level comparison ───────────────────────────────────────────
        double pJW  = 1.0 - JaroWinkler(speech, productName);
        double pLev = (double)Levenshtein(speech, productName)
                     / Math.Max(speech.Length, productName.Length);
        double phrase = pJW * 0.6 + pLev * 0.4;

        // ── Token coverage bonus (up to 25 % reduction) ───────────────────────
        double coverage = TokenCoverageScore(sw, pw);

        double raw = Math.Min(Math.Min(bestWord, bestNgram), phrase);
        return raw * (1.0 - coverage * 0.25);
    }

    // Score a single word pair: JaroWinkler + Levenshtein + Soundex
    private static double WordScore(string s, string p)
    {
        if (s == p) return 0.0;
        double jw      = 1.0 - JaroWinkler(s, p);
        double lev     = (double)Levenshtein(s, p) / Math.Max(s.Length, p.Length);
        double soundex = Soundex(s) == Soundex(p) ? 0.0 : 0.4;
        return jw * 0.5 + lev * 0.3 + soundex * 0.2;
    }

    // Fraction of product tokens that are phonetically "covered" by speech tokens
    private static double TokenCoverageScore(string[] speechWords, string[] productWords)
    {
        if (productWords.Length == 0) return 0.0;
        int covered = 0;
        foreach (var pw in productWords) {
            if (pw.Length < 2) continue;
            if (speechWords.Any(sw => sw == pw ||
                (sw.Length >= 3 && WordScore(sw, pw) < 0.25)))
                covered++;
        }
        return (double)covered / productWords.Length;
    }

    private IActionResult FoundProduct(Product p) =>
        p.StockQuantity > 0
            ? Json(new { found=true, available=true,  productId=p.Id, productName=p.Name,
                         productPrice=p.Price.ToString("F2"), suggestions=(object?)null })
            : Json(new { found=true, available=false, productId=p.Id, productName=p.Name,
                         productPrice=p.Price.ToString("F2"), suggestions=(object?)null });

    // ════════════════════════════════════════════════════════════════════════
    //  JARO-WINKLER SIMILARITY  (1.0 = identical, handles transpositions)
    // ════════════════════════════════════════════════════════════════════════
    private static double JaroWinkler(string s1, string s2)
    {
        if (s1 == s2) return 1.0;
        int l1 = s1.Length, l2 = s2.Length;
        if (l1 == 0 || l2 == 0) return 0.0;
        int md = Math.Max(l1, l2) / 2 - 1; if (md < 0) md = 0;
        var m1 = new bool[l1]; var m2 = new bool[l2];
        int matches = 0;
        for (int i = 0; i < l1; i++) {
            int lo = Math.Max(0, i - md), hi = Math.Min(i + md + 1, l2);
            for (int j = lo; j < hi; j++) {
                if (m2[j] || s1[i] != s2[j]) continue;
                m1[i] = m2[j] = true; matches++; break;
            }
        }
        if (matches == 0) return 0.0;
        int trans = 0, k = 0;
        for (int i = 0; i < l1; i++) {
            if (!m1[i]) continue;
            while (!m2[k]) k++;
            if (s1[i] != s2[k]) trans++;
            k++;
        }
        double jaro = (matches/(double)l1 + matches/(double)l2
                       + (matches - trans/2.0)/matches) / 3.0;
        int prefix = 0;
        for (int i = 0; i < Math.Min(4, Math.Min(l1, l2)); i++) {
            if (s1[i] == s2[i]) prefix++; else break;
        }
        return jaro + prefix * 0.1 * (1.0 - jaro);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  SOUNDEX  — phonetic hash, groups acoustically similar words
    //  e.g. "tumato" → T530, "tomato" → T530 (same code → bonus)
    // ════════════════════════════════════════════════════════════════════════
    private static string Soundex(string word)
    {
        if (string.IsNullOrEmpty(word)) return "0000";
        word = word.ToUpper();
        var code = new Dictionary<char, char> {
            ['B']='1',['F']='1',['P']='1',['V']='1',
            ['C']='2',['G']='2',['J']='2',['K']='2',['Q']='2',['S']='2',['X']='2',['Z']='2',
            ['D']='3',['T']='3',['L']='4',['M']='5',['N']='5',['R']='6',
        };
        var sb = new System.Text.StringBuilder(); sb.Append(word[0]);
        char prev = code.TryGetValue(word[0], out var pv) ? pv : '0';
        for (int i = 1; i < word.Length && sb.Length < 4; i++) {
            if (code.TryGetValue(word[i], out var c) && c != prev) { sb.Append(c); prev = c; }
            else if (!code.ContainsKey(word[i])) prev = '0';
        }
        while (sb.Length < 4) sb.Append('0');
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  LEVENSHTEIN EDIT DISTANCE
    // ════════════════════════════════════════════════════════════════════════
    private static int Levenshtein(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
        if (string.IsNullOrEmpty(t)) return s.Length;
        int m = s.Length, n = t.Length;
        var d = new int[m + 1, n + 1];
        for (int i = 0; i <= m; i++) d[i, 0] = i;
        for (int j = 0; j <= n; j++) d[0, j] = j;
        for (int i = 1; i <= m; i++)
            for (int j = 1; j <= n; j++) {
                int cost = s[i-1] == t[j-1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i-1,j]+1, d[i,j-1]+1), d[i-1,j-1]+cost);
            }
        return d[m, n];
    }
}

public class VoiceRequest
{
    public string?       Speech       { get; set; }
    public List<string>? Alternatives { get; set; }
    public List<double>? Confidences  { get; set; }
}
