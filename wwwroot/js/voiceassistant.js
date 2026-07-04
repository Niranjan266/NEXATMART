/**
 * Nexamart Voice Assistant — NLP v4
 * All emoji replaced with inline SVG animations keyed by state.
 */
(function () {
    'use strict';

    const SpeechRec      = window.SpeechRecognition || window.webkitSpeechRecognition;
    const MIN_CONFIDENCE = 0.25;
    const MAX_RETRIES    = 2;

    const ctx = {
        lastProductId:   null,
        lastProductName: null,
        lastSuggestions: [],
        lastQuantity:    1,
        retryCount:      0,
    };

    let isListening = false;
    let overlay     = null;

    // ══════════════════════════════════════════════════════════════════
    //  SVG ANIMATION LIBRARY  (no emoji — each state has its own SVG)
    // ══════════════════════════════════════════════════════════════════
    var ANIM = {

        /* Listening — microphone with 3 expanding ripple rings */
        mic: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
           + '<circle cx="50" cy="50" r="36" fill="none" stroke="#c4b5fd" stroke-width="1.5">'
           +   '<animate attributeName="r" values="36;50" dur="1.6s" repeatCount="indefinite"/>'
           +   '<animate attributeName="opacity" values="0.6;0" dur="1.6s" repeatCount="indefinite"/>'
           + '</circle>'
           + '<circle cx="50" cy="50" r="36" fill="none" stroke="#c4b5fd" stroke-width="1.5">'
           +   '<animate attributeName="r" values="36;50" dur="1.6s" begin="0.53s" repeatCount="indefinite"/>'
           +   '<animate attributeName="opacity" values="0.6;0" dur="1.6s" begin="0.53s" repeatCount="indefinite"/>'
           + '</circle>'
           + '<circle cx="50" cy="50" r="36" fill="none" stroke="#c4b5fd" stroke-width="1.5">'
           +   '<animate attributeName="r" values="36;50" dur="1.6s" begin="1.06s" repeatCount="indefinite"/>'
           +   '<animate attributeName="opacity" values="0.6;0" dur="1.6s" begin="1.06s" repeatCount="indefinite"/>'
           + '</circle>'
           + '<circle cx="50" cy="50" r="34" fill="#7c3aed"/>'
           + '<rect x="41" y="24" width="18" height="28" rx="9" fill="white"/>'
           + '<path d="M32 48 Q32 67 50 67 Q68 67 68 48" stroke="white" stroke-width="3.5" fill="none" stroke-linecap="round"/>'
           + '<line x1="50" y1="67" x2="50" y2="76" stroke="white" stroke-width="3.5" stroke-linecap="round"/>'
           + '<line x1="41" y1="76" x2="59" y2="76" stroke="white" stroke-width="3.5" stroke-linecap="round"/>'
           + '</svg>',

        /* Hearing — 5 equalizer bars animating */
        hear: '<svg viewBox="0 0 104 64" xmlns="http://www.w3.org/2000/svg">'
            + '<rect x="4"  y="32" width="14" height="20" rx="5" fill="#7c3aed">'
            +   '<animate attributeName="height" values="20;52;12;44;20" dur="1.1s" repeatCount="indefinite"/>'
            +   '<animate attributeName="y"      values="32;6;44;12;32"  dur="1.1s" repeatCount="indefinite"/>'
            + '</rect>'
            + '<rect x="23" y="22" width="14" height="30" rx="5" fill="#a855f7">'
            +   '<animate attributeName="height" values="30;14;56;18;30" dur="1.1s" begin="0.18s" repeatCount="indefinite"/>'
            +   '<animate attributeName="y"      values="22;38;4;34;22"  dur="1.1s" begin="0.18s" repeatCount="indefinite"/>'
            + '</rect>'
            + '<rect x="42" y="10" width="14" height="44" rx="5" fill="#7c3aed">'
            +   '<animate attributeName="height" values="44;60;22;50;44" dur="1.1s" begin="0.09s" repeatCount="indefinite"/>'
            +   '<animate attributeName="y"      values="10;2;32;6;10"   dur="1.1s" begin="0.09s" repeatCount="indefinite"/>'
            + '</rect>'
            + '<rect x="61" y="18" width="14" height="36" rx="5" fill="#a855f7">'
            +   '<animate attributeName="height" values="36;10;54;24;36" dur="1.1s" begin="0.27s" repeatCount="indefinite"/>'
            +   '<animate attributeName="y"      values="18;42;6;28;18"  dur="1.1s" begin="0.27s" repeatCount="indefinite"/>'
            + '</rect>'
            + '<rect x="80" y="28" width="14" height="24" rx="5" fill="#7c3aed">'
            +   '<animate attributeName="height" values="24;48;16;40;24" dur="1.1s" begin="0.14s" repeatCount="indefinite"/>'
            +   '<animate attributeName="y"      values="28;4;36;12;28"  dur="1.1s" begin="0.14s" repeatCount="indefinite"/>'
            + '</rect>'
            + '</svg>',

        /* Processing — 3 dots bouncing (NLP thinking) */
        think: '<svg viewBox="0 0 100 50" xmlns="http://www.w3.org/2000/svg">'
             + '<circle cx="18" cy="25" r="9" fill="#7c3aed">'
             +   '<animate attributeName="cy" values="25;9;25"  dur="0.85s" repeatCount="indefinite"/>'
             +   '<animate attributeName="r"  values="9;7;9"    dur="0.85s" repeatCount="indefinite"/>'
             + '</circle>'
             + '<circle cx="50" cy="25" r="9" fill="#a855f7">'
             +   '<animate attributeName="cy" values="25;9;25"  dur="0.85s" begin="0.28s" repeatCount="indefinite"/>'
             +   '<animate attributeName="r"  values="9;7;9"    dur="0.85s" begin="0.28s" repeatCount="indefinite"/>'
             + '</circle>'
             + '<circle cx="82" cy="25" r="9" fill="#7c3aed">'
             +   '<animate attributeName="cy" values="25;9;25"  dur="0.85s" begin="0.56s" repeatCount="indefinite"/>'
             +   '<animate attributeName="r"  values="9;7;9"    dur="0.85s" begin="0.56s" repeatCount="indefinite"/>'
             + '</circle>'
             + '</svg>',

        /* Searching — magnifying glass with animated arc */
        search: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
              + '<circle cx="42" cy="42" r="24" fill="none" stroke="#ede9fe" stroke-width="7"/>'
              + '<circle cx="42" cy="42" r="24" fill="none" stroke="#7c3aed" stroke-width="7"'
              +   ' stroke-dasharray="150" stroke-dashoffset="0" stroke-linecap="round">'
              +   '<animate attributeName="stroke-dashoffset" values="0;-150;0" dur="1.4s" repeatCount="indefinite"/>'
              + '</circle>'
              + '<line x1="60" y1="60" x2="80" y2="80" stroke="#7c3aed" stroke-width="7" stroke-linecap="round"/>'
              + '</svg>',

        /* Success — green circle + animated check */
        ok: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
          + '<circle cx="50" cy="50" r="44" fill="#10b981">'
          +   '<animate attributeName="r" values="0;44" dur="0.38s" fill="freeze"/>'
          + '</circle>'
          + '<polyline points="27,50 43,67 73,35" fill="none" stroke="white" stroke-width="6"'
          +   ' stroke-linecap="round" stroke-linejoin="round"'
          +   ' stroke-dasharray="75" stroke-dashoffset="75">'
          +   '<animate attributeName="stroke-dashoffset" values="75;0" dur="0.45s" begin="0.3s" fill="freeze"/>'
          + '</polyline>'
          + '</svg>',

        /* Warning / suggestions — amber circle + question mark */
        warn: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
            + '<circle cx="50" cy="50" r="44" fill="#f59e0b"/>'
            + '<text x="50" y="68" text-anchor="middle" font-size="54" font-weight="900"'
            +   ' fill="white" font-family="Arial, sans-serif">?</text>'
            + '</svg>',

        /* Error / not found — red circle + X */
        fail: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
            + '<circle cx="50" cy="50" r="44" fill="#ef4444"/>'
            + '<line x1="32" y1="32" x2="68" y2="68" stroke="white" stroke-width="7" stroke-linecap="round">'
            +   '<animate attributeName="stroke-dashoffset" values="50;0" dur="0.25s" fill="freeze"'
            +     ' stroke-dasharray="50"/>'
            + '</line>'
            + '<line x1="68" y1="32" x2="32" y2="68" stroke="white" stroke-width="7" stroke-linecap="round"/>'
            + '</svg>',

        /* Cart — purple circle with cart icon + bouncing product dot */
        cart: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
            + '<circle cx="50" cy="50" r="44" fill="#7c3aed"/>'
            + '<path d="M20 30 L27 30 L37 63 L68 63 L78 40 L31 40"'
            +   ' stroke="white" stroke-width="4.5" fill="none" stroke-linecap="round" stroke-linejoin="round"/>'
            + '<circle cx="41" cy="71" r="5" fill="white"/>'
            + '<circle cx="63" cy="71" r="5" fill="white"/>'
            + '<circle cx="55" cy="25" r="7" fill="#fbbf24">'
            +   '<animate attributeName="cy" values="25;53;25" dur="0.75s" repeatCount="indefinite"'
            +     ' calcMode="spline" keySplines="0.4,0,0.6,1;0.4,0,0.6,1"/>'
            + '</circle>'
            + '</svg>',

        /* Delivery — truck with horizontal slide */
        truck: '<svg viewBox="0 0 120 80" xmlns="http://www.w3.org/2000/svg">'
             + '<g>'
             +   '<animateTransform attributeName="transform" type="translate"'
             +     ' values="0,0;8,0;0,0" dur="0.9s" repeatCount="indefinite"/>'
             +   '<rect x="8"  y="18" width="56" height="36" rx="5" fill="#7c3aed"/>'
             +   '<rect x="64" y="28" width="30" height="26" rx="5" fill="#6d28d9"/>'
             +   '<rect x="67" y="31" width="20" height="13" rx="3" fill="#c4b5fd"/>'
             +   '<circle cx="22" cy="57" r="9" fill="#1f2937"/>'
             +   '<circle cx="22" cy="57" r="5" fill="#9ca3af"/>'
             +   '<circle cx="78" cy="57" r="9" fill="#1f2937"/>'
             +   '<circle cx="78" cy="57" r="5" fill="#9ca3af"/>'
             +   '<line x1="2"  y1="40" x2="10" y2="40" stroke="#c4b5fd" stroke-width="2.5"'
             +     ' stroke-linecap="round" opacity="0.7"/>'
             +   '<line x1="2"  y1="48" x2="8"  y2="48" stroke="#c4b5fd" stroke-width="2.5"'
             +     ' stroke-linecap="round" opacity="0.5"/>'
             + '</g>'
             + '</svg>',

        /* Out of stock — grey circle + empty box */
        empty: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
             + '<circle cx="50" cy="50" r="44" fill="#6b7280"/>'
             + '<rect x="28" y="40" width="44" height="30" rx="3" fill="none" stroke="white" stroke-width="3.5"/>'
             + '<polyline points="28,50 50,50 72,50" stroke="white" stroke-width="3.5" fill="none"/>'
             + '<line x1="50" y1="40" x2="50" y2="50" stroke="white" stroke-width="3.5"/>'
             + '<line x1="35" y1="60" x2="65" y2="60" stroke="white" stroke-width="2.5"'
             +   ' stroke-dasharray="5 4" opacity="0.55"/>'
             + '<line x1="35" y1="67" x2="55" y2="67" stroke="white" stroke-width="2.5"'
             +   ' stroke-dasharray="5 4" opacity="0.35"/>'
             + '</svg>',

        /* Category browse — animated 4-tile grid */
        grid: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
            + '<circle cx="50" cy="50" r="44" fill="#7c3aed"/>'
            + '<rect x="26" y="26" width="20" height="20" rx="4" fill="white" opacity="0.9"/>'
            + '<rect x="54" y="26" width="20" height="20" rx="4" fill="white" opacity="0.6">'
            +   '<animate attributeName="opacity" values="0.6;1;0.6" dur="1.2s" repeatCount="indefinite"/>'
            + '</rect>'
            + '<rect x="26" y="54" width="20" height="20" rx="4" fill="white" opacity="0.6">'
            +   '<animate attributeName="opacity" values="0.6;1;0.6" dur="1.2s" begin="0.4s" repeatCount="indefinite"/>'
            + '</rect>'
            + '<rect x="54" y="54" width="20" height="20" rx="4" fill="white" opacity="0.6">'
            +   '<animate attributeName="opacity" values="0.6;1;0.6" dur="1.2s" begin="0.8s" repeatCount="indefinite"/>'
            + '</rect>'
            + '</svg>',

        /* Mic blocked — mic with X overlay */
        blocked: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
               + '<circle cx="50" cy="50" r="44" fill="#ef4444"/>'
               + '<rect x="40" y="22" width="20" height="32" rx="10" fill="white" opacity="0.9"/>'
               + '<path d="M30 46 Q30 64 50 64 Q70 64 70 46" stroke="white" stroke-width="3.5"'
               +   ' fill="none" stroke-linecap="round"/>'
               + '<line x1="50" y1="64" x2="50" y2="72" stroke="white" stroke-width="3.5" stroke-linecap="round"/>'
               + '<line x1="42" y1="72" x2="58" y2="72" stroke="white" stroke-width="3.5" stroke-linecap="round"/>'
               + '<line x1="30" y1="30" x2="70" y2="70" stroke="#ef4444" stroke-width="5" stroke-linecap="round"/>'
               + '<line x1="30" y1="30" x2="70" y2="70" stroke="white" stroke-width="4" stroke-linecap="round"'
               +   ' opacity="0.85"/>'
               + '</svg>',

        /* Network error — signal bars crossed out */
        network: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
               + '<circle cx="50" cy="50" r="44" fill="#f59e0b"/>'
               + '<rect x="22" y="62" width="10" height="15" rx="2" fill="white" opacity="0.4"/>'
               + '<rect x="38" y="52" width="10" height="25" rx="2" fill="white" opacity="0.6"/>'
               + '<rect x="54" y="40" width="10" height="37" rx="2" fill="white" opacity="0.5"/>'
               + '<rect x="70" y="26" width="10" height="51" rx="2" fill="white" opacity="0.4"/>'
               + '<line x1="28" y1="28" x2="72" y2="72" stroke="white" stroke-width="5" stroke-linecap="round"/>'
               + '</svg>',

        /* Retry — rotating arrows */
        retry: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
             + '<circle cx="50" cy="50" r="44" fill="#7c3aed"/>'
             + '<g>'
             +   '<animateTransform attributeName="transform" type="rotate"'
             +     ' values="0 50 50;360 50 50" dur="1s" repeatCount="indefinite"/>'
             +   '<path d="M50 20 A30 30 0 1 1 26 65" stroke="white" stroke-width="6" fill="none"'
             +     ' stroke-linecap="round"/>'
             +   '<polygon points="20,72 30,58 36,72" fill="white"/>'
             + '</g>'
             + '</svg>',

        /* Silence / no speech — mic with flat line */
        silence: '<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">'
               + '<circle cx="50" cy="50" r="44" fill="#9ca3af"/>'
               + '<rect x="41" y="24" width="18" height="28" rx="9" fill="white" opacity="0.6"/>'
               + '<path d="M32 48 Q32 67 50 67 Q68 67 68 48" stroke="white" stroke-width="3.5"'
               +   ' fill="none" stroke-linecap="round" opacity="0.6"/>'
               + '<line x1="50" y1="67" x2="50" y2="76" stroke="white" stroke-width="3.5"'
               +   ' stroke-linecap="round" opacity="0.6"/>'
               + '<line x1="41" y1="76" x2="59" y2="76" stroke="white" stroke-width="3.5"'
               +   ' stroke-linecap="round" opacity="0.6"/>'
               + '<line x1="26" y1="50" x2="74" y2="50" stroke="white" stroke-width="3" stroke-linecap="round"'
               +   ' stroke-dasharray="6 4" opacity="0.5">'
               +   '<animate attributeName="opacity" values="0.5;0.15;0.5" dur="1.5s" repeatCount="indefinite"/>'
               + '</line>'
               + '</svg>',
    };

    // ══════════════════════════════════════════════════════════════════
    //  STYLES
    // ══════════════════════════════════════════════════════════════════
    function injectStyles() {
        if (document.getElementById('va-styles')) return;
        var s = document.createElement('style');
        s.id = 'va-styles';
        s.textContent =
            '#va-overlay{position:fixed;inset:0;z-index:99999;display:flex;align-items:center;justify-content:center}' +
            '.va-backdrop{position:absolute;inset:0;background:rgba(15,5,30,.62);backdrop-filter:blur(8px)}' +
            '.va-panel{position:relative;background:#fff;border-radius:28px;overflow:hidden;' +
                'box-shadow:0 32px 80px rgba(0,0,0,.32);min-width:300px;max-width:420px;width:92%;' +
                'animation:va-pop .24s cubic-bezier(.34,1.56,.64,1)}' +
            '@keyframes va-pop{from{transform:scale(.78);opacity:0}to{transform:scale(1);opacity:1}}' +
            '.va-header{background:linear-gradient(135deg,#7c3aed 0%,#a855f7 100%);' +
                'padding:2rem 1.5rem 1.5rem;display:flex;flex-direction:column;align-items:center;gap:.75rem}' +
            '.va-anim{width:96px;height:96px;display:flex;align-items:center;justify-content:center;' +
                'transition:opacity .25s}' +
            '.va-anim svg{width:100%;height:100%}' +
            '.va-status{font-size:1rem;font-weight:700;color:white;text-align:center;letter-spacing:.01em}' +
            '.va-body{padding:1rem 1.5rem 1.5rem}' +
            '.va-transcript{font-size:.84rem;color:#6b7280;font-style:italic;text-align:center;' +
                'min-height:1.2rem;margin-bottom:.9rem;transition:opacity .2s}' +
            '.va-chips{display:flex;flex-wrap:wrap;gap:.45rem;justify-content:center;margin-bottom:.9rem}' +
            '.va-chip{background:#f3e8ff;color:#7c3aed;border:1.5px solid #d8b4fe;' +
                'border-radius:50px;padding:.3rem .9rem;font-size:.8rem;font-weight:600;' +
                'cursor:pointer;transition:background .15s,transform .12s;white-space:nowrap}' +
            '.va-chip:hover{background:#e9d5ff;transform:scale(1.04)}' +
            '.va-chip.browse{background:#fff1f2;color:#be123c;border-color:#fda4af}' +
            '.va-close{display:block;width:100%;background:none;border:none;border-top:1px solid #f3f4f6;' +
                'padding:.85rem;cursor:pointer;color:#9ca3af;font-size:.84rem;font-weight:500;' +
                'transition:background .15s,color .15s}' +
            '.va-close:hover{background:#f9fafb;color:#374151}';
        document.head.appendChild(s);
    }

    // ══════════════════════════════════════════════════════════════════
    //  OVERLAY
    // ══════════════════════════════════════════════════════════════════
    function createOverlay() {
        injectStyles();
        if (overlay) overlay.remove();
        var div = document.createElement('div');
        div.id = 'va-overlay';
        div.innerHTML =
            '<div class="va-backdrop"></div>' +
            '<div class="va-panel">' +
              '<div class="va-header">' +
                '<div class="va-anim" id="va-anim"></div>' +
                '<div class="va-status" id="va-status">Getting ready</div>' +
              '</div>' +
              '<div class="va-body">' +
                '<div class="va-transcript" id="va-transcript"></div>' +
                '<div class="va-chips" id="va-chips"></div>' +
              '</div>' +
              '<button class="va-close" id="va-close">Cancel</button>' +
            '</div>';
        document.body.appendChild(div);
        overlay = div;
        document.getElementById('va-close').onclick = closeOverlay;
    }

    function el(id) { return overlay ? document.getElementById(id) : null; }

    function setStatus(animKey, status, transcript) {
        if (!overlay) return;
        var anim = el('va-anim');
        if (anim && ANIM[animKey]) anim.innerHTML = ANIM[animKey];
        var st = el('va-status');
        if (st) st.textContent = status;
        if (transcript !== undefined) {
            var t = el('va-transcript');
            if (t) t.textContent = transcript;
        }
    }

    function clearChips() { var c = el('va-chips'); if (c) c.innerHTML = ''; }

    function renderChips(items, browseId) {
        var container = el('va-chips');
        if (!container) return;
        container.innerHTML = '';
        (items || []).forEach(function(item) {
            var btn = document.createElement('button');
            btn.className   = 'va-chip';
            btn.textContent = item.name + (item.price ? '  ₹' + item.price : '');
            btn.onclick = function() {
                setStatus('cart', 'Adding ' + item.name, '');
                clearChips();
                speak('Adding ' + item.name + ' to your cart!', function() {
                    window.location.href = '/Cart/AddToCart?productId=' + item.id;
                });
            };
            container.appendChild(btn);
        });
        if (browseId && browseId > 0) {
            var b = document.createElement('button');
            b.className   = 'va-chip browse';
            b.textContent = 'Browse All';
            b.onclick = function() { window.location.href = '/Home/Index?categoryId=' + browseId; };
            container.appendChild(b);
        }
    }

    function closeOverlay() {
        isListening = false;
        ctx.retryCount = 0;
        window.speechSynthesis.cancel();
        if (overlay) { overlay.remove(); overlay = null; }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SPEECH SYNTHESIS
    // ══════════════════════════════════════════════════════════════════
    function speak(text, callback) {
        window.speechSynthesis.cancel();
        var utt  = new SpeechSynthesisUtterance(text);
        utt.lang = 'en-US'; utt.rate = 0.93;
        if (callback) utt.onend = callback;
        setTimeout(function() { window.speechSynthesis.speak(utt); }, 60);
    }

    // ══════════════════════════════════════════════════════════════════
    //  QUANTITY EXTRACTION
    // ══════════════════════════════════════════════════════════════════
    var WORD_NUMS = {one:1,two:2,three:3,four:4,five:5,six:6,seven:7,eight:8,nine:9,ten:10};

    function extractQuantity(text) {
        var numMatch = text.match(/\b([2-9]|1[0-9]|20)\b/);
        if (numMatch) return { qty: parseInt(numMatch[1]), text: text.replace(numMatch[0], '').trim() };
        for (var word in WORD_NUMS) {
            var re = new RegExp('\\b' + word + '\\b', 'i');
            if (re.test(text)) return { qty: WORD_NUMS[word], text: text.replace(re, '').trim() };
        }
        return { qty: 1, text: text };
    }

    // ══════════════════════════════════════════════════════════════════
    //  INTENT DETECTION (English + Tamil romanized)
    // ══════════════════════════════════════════════════════════════════
    var INTENT_MAP = [
        { name:'placeOrder',    kws:['place order','confirm order','submit order','complete order','finalize order','proceed to pay','buy now','pay now','make payment','order pannunga','order pannuvom'] },
        { name:'checkout',      kws:['go to checkout','proceed to checkout','checkout','check out','take me to checkout','bill pannunga','bill podu'] },
        { name:'orderHistory',  kws:['order history','my orders','past orders','previous orders','what i ordered','track order','show orders','show my orders','en order','order parunga'] },
        { name:'cart',          kws:["what's in my cart",'what is in my cart','view my cart','open my cart','show my cart','my shopping cart','my cart','my bag','open cart','en cart','cart parunga'] },
        { name:'admin',         kws:['admin panel','go to admin','open admin','administration panel','admin'] },
        { name:'logout',        kws:['sign me out','log me out','sign out','log out','logout','exit account','veliyeru'] },
        { name:'login',         kws:['sign me in','log me in','create account','register','sign in','log in','login','ulle vaa','login pannunga'] },
        { name:'home',          kws:['go to home page','take me home','go to home','go home','home page','main page','home ku poo','mudal pakkam'] },
        { name:'delivery',      kws:['when will my order arrive','when will i get my order','how long will delivery take','delivery time','shipping time','when will it arrive','how long','delivery','when will i get','eppo varum','delivery eppo'] },
        { name:'contextAdd',    kws:['add it','add that','that one','yes that','i will take it',"i'll take it",'take it','add to cart','itha vangunga','itha add pannunga'] },
        { name:'contextFirst',  kws:['first one','number one','the first','option one','first option','first product','mudhalil ulla','onravathu'] },
        { name:'contextSecond', kws:['second one','number two','the second','option two','second option','second product','irendavathu'] },
        { name:'contextThird',  kws:['third one','number three','the third','option three','third option','third product','moondravathu'] },
    ];

    function detectIntent(text) {
        var lower = text.toLowerCase().trim();
        var best = null, bestLen = 0;
        for (var i = 0; i < INTENT_MAP.length; i++) {
            var intent = INTENT_MAP[i];
            for (var j = 0; j < intent.kws.length; j++) {
                var kw = intent.kws[j];
                if (lower.indexOf(kw) !== -1 && kw.length > bestLen) {
                    bestLen = kw.length; best = intent.name;
                }
            }
        }
        return bestLen >= 4 ? best : null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  ENTRY POINT
    // ══════════════════════════════════════════════════════════════════
    function startListening() {
        if (!SpeechRec) {
            alert('Voice recognition requires Google Chrome. Please open the site in Chrome.');
            return;
        }
        if (isListening) return;
        isListening    = true;
        ctx.retryCount = 0;
        createOverlay();
        setStatus('mic', 'Getting ready', '');
        speak('Hi! What would you like today? You can speak in English or Tamil.', beginRecognition);
    }

    // ══════════════════════════════════════════════════════════════════
    //  RECOGNITION ENGINE
    // ══════════════════════════════════════════════════════════════════
    function beginRecognition() {
        if (!overlay) return;
        clearChips();
        setStatus('mic', 'Listening… tell me what you need', '');

        var rec             = new SpeechRec();
        rec.lang            = 'en-US';
        rec.continuous      = false;
        rec.interimResults  = true;
        rec.maxAlternatives = 6;

        rec.onstart = function() {
            isListening = true;
            setStatus('mic', 'Listening… tell me what you need', '');
        };

        rec.onresult = function(event) {
            var interim = '', final = '', alts = [];
            for (var r = 0; r < event.results.length; r++) {
                var result = event.results[r];
                if (result.isFinal) {
                    final += result[0].transcript;
                    for (var a = 0; a < result.length; a++)
                        alts.push({ text: result[a].transcript.trim(), conf: result[a].confidence || 1.0 });
                } else {
                    interim += result[0].transcript;
                }
            }
            if (interim) setStatus('hear', 'Hearing…', interim);
            if (final.trim()) {
                var primaryConf = alts.length > 0 ? alts[0].conf : 1.0;
                if (primaryConf < MIN_CONFIDENCE && ctx.retryCount < MAX_RETRIES) {
                    ctx.retryCount++;
                    setStatus('silence', "Didn't catch that clearly", 'Please say it again a bit louder');
                    speak("Sorry, I didn't quite catch that. Could you say it again?", beginRecognition);
                    return;
                }
                ctx.retryCount = 0;
                setStatus('think', 'Processing…', '"' + final.trim() + '"');
                handleSpeech(final.trim(), alts);
            }
        };

        rec.onerror = function(event) {
            isListening = false;
            switch (event.error) {
                case 'no-speech':
                    if (ctx.retryCount < MAX_RETRIES) {
                        ctx.retryCount++;
                        setStatus('silence', "I didn't hear anything", 'Please speak after the prompt');
                        setTimeout(function() { if (overlay) beginRecognition(); }, 1800);
                    } else {
                        ctx.retryCount = 0;
                        setStatus('silence', 'Still no speech detected', 'Tap Cancel or try again');
                    }
                    break;
                case 'not-allowed':
                case 'audio-capture':
                    setStatus('blocked', 'Microphone blocked', 'Allow mic access in browser settings, then try again');
                    setTimeout(closeOverlay, 4500);
                    break;
                case 'network':
                    setStatus('network', 'Network error', 'Check your internet connection');
                    setTimeout(closeOverlay, 3000);
                    break;
                case 'aborted':
                    break;
                default:
                    ctx.retryCount++;
                    if (ctx.retryCount <= MAX_RETRIES) {
                        setStatus('retry', 'Restarting…', '');
                        setTimeout(function() { if (overlay) beginRecognition(); }, 1200);
                    }
            }
        };

        rec.onnomatch = function() {
            isListening = false;
            setStatus('warn', "Couldn't understand", 'Please speak clearly and try again');
            setTimeout(function() { if (overlay) beginRecognition(); }, 2200);
        };

        rec.onend = function() { isListening = false; };

        try { rec.start(); }
        catch(e) { setStatus('fail', 'Mic error — please try again', ''); isListening = false; }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SPEECH ROUTER
    // ══════════════════════════════════════════════════════════════════
    function handleSpeech(text, alts) {
        var allTexts = [text].concat(alts.map(function(a) { return a.text; }));
        var intent   = null;
        for (var i = 0; i < allTexts.length; i++) {
            intent = detectIntent(allTexts[i]);
            if (intent) break;
        }

        if (intent === 'contextAdd') {
            if (ctx.lastProductId) {
                setStatus('cart', 'Adding ' + ctx.lastProductName, '');
                speak('Adding ' + ctx.lastProductName + ' to your cart!', function() {
                    window.location.href = '/Cart/AddToCart?productId=' + ctx.lastProductId;
                });
            } else if (ctx.lastSuggestions.length > 0) {
                addSuggestion(ctx.lastSuggestions[0]);
            } else {
                speak("I'm not sure which product to add. Could you say the product name?", beginRecognition);
            }
            return;
        }
        if (intent === 'contextFirst'  && ctx.lastSuggestions[0]) { addSuggestion(ctx.lastSuggestions[0]); return; }
        if (intent === 'contextSecond' && ctx.lastSuggestions[1]) { addSuggestion(ctx.lastSuggestions[1]); return; }
        if (intent === 'contextThird'  && ctx.lastSuggestions[2]) { addSuggestion(ctx.lastSuggestions[2]); return; }
        if (intent) { handleIntent(intent); return; }

        var extracted    = extractQuantity(text);
        ctx.lastQuantity = extracted.qty;

        setStatus('search', 'Searching…', '"' + text + '"');
        var sortedAlts = alts.slice().sort(function(a, b) { return b.conf - a.conf; });

        fetch('/Voice/search', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                speech:       extracted.text,
                alternatives: sortedAlts.map(function(a) { return a.text; }),
                confidences:  sortedAlts.map(function(a) { return a.conf; }),
            }),
        })
        .then(function(r) { if (!r.ok) throw new Error('server'); return r.json(); })
        .then(function(data) { handleSearchResult(data); })
        .catch(function() {
            setStatus('network', 'Connection error', 'Please try again');
            setTimeout(closeOverlay, 2500);
        });
    }

    function addSuggestion(item) {
        setStatus('cart', 'Adding ' + item.name, '');
        clearChips();
        speak('Adding ' + item.name + ' to your cart!', function() {
            window.location.href = '/Cart/AddToCart?productId=' + item.id;
        });
    }

    // ══════════════════════════════════════════════════════════════════
    //  NAVIGATION INTENTS
    // ══════════════════════════════════════════════════════════════════
    var NAV = {
        placeOrder:   ['Placing your order now!',       '/Cart/Checkout'],
        checkout:     ['Going to checkout.',             '/Cart/Checkout'],
        orderHistory: ['Opening your order history.',   '/Account/OrderHistory'],
        cart:         ['Opening your cart.',             '/Cart/Index'],
        admin:        ['Opening the admin panel.',       '/Admin/Index'],
        logout:       ['Signing you out. Goodbye!',      '/Account/Logout'],
        login:        ['Opening the login page.',        '/Account/Login'],
        home:         ['Going to the home page.',        '/Home/Index'],
    };

    function handleIntent(intent) {
        if (intent === 'delivery') {
            setStatus('truck', 'Delivery Info', '');
            speak('Your order will be delivered within 30 to 45 minutes. Thank you for shopping with Nexamart!', closeOverlay);
            return;
        }
        var nav = NAV[intent];
        if (nav) {
            setStatus('ok', nav[0], '');
            speak(nav[0], function() { window.location.href = nav[1]; });
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  SEARCH RESULT HANDLER
    // ══════════════════════════════════════════════════════════════════
    function handleSearchResult(data) {
        if (data.isDeliveryInfo) {
            setStatus('truck', 'Delivery Info', '');
            speak(data.message, closeOverlay);
            return;
        }
        if (data.isCheckout) {
            setStatus('cart', data.found ? 'Cart Summary' : 'Cart Empty', '');
            speak(data.message, function() {
                if (data.found) listenForCheckoutConfirmation();
                else closeOverlay();
            });
            return;
        }
        if (data.isCategory) {
            var items = Array.isArray(data.suggestions) ? data.suggestions : [];
            ctx.lastSuggestions = items;
            setStatus('grid', 'Category Results', '');
            if (items.length > 0) renderChips(items, data.categoryId);
            speak(data.message, function() {
                setTimeout(function() { if (overlay) beginRecognition(); }, 400);
            });
            return;
        }
        if (data.found) {
            if (data.available) {
                var price  = parseFloat(data.productPrice || 0).toFixed(0);
                var qtyStr = ctx.lastQuantity > 1 ? ' ' + ctx.lastQuantity + ' of' : '';
                var msg    = 'Found' + qtyStr + ' ' + data.productName + ' for ₹' + price + '. Adding to your cart!';
                ctx.lastProductId   = data.productId;
                ctx.lastProductName = data.productName;
                ctx.lastSuggestions = [];
                setStatus('ok', 'Found it!', data.productName);
                speak(msg, function() { window.location.href = '/Cart/AddToCart?productId=' + data.productId; });
            } else {
                ctx.lastProductId = null; ctx.lastProductName = null;
                setStatus('empty', 'Out of Stock', data.productName);
                speak('Sorry, ' + data.productName + ' is out of stock right now. Want to try something else?', function() {
                    setTimeout(function() { if (overlay) beginRecognition(); }, 500);
                });
            }
            return;
        }
        var suggestions = Array.isArray(data.suggestions) ? data.suggestions : [];
        if (suggestions.length > 0) {
            ctx.lastSuggestions = suggestions;
            var labels   = ['first', 'second', 'third'];
            var readable = suggestions.map(function(s, i) {
                return (labels[i] || (i+1) + '.') + ' ' + s.name;
            }).join(', or ');
            setStatus('warn', 'Did you mean…?', suggestions.map(function(s) { return s.name; }).join(' / '));
            renderChips(suggestions, 0);
            speak('I found some close matches: ' + readable + '. Tap one, or say first one, second one, and so on.', function() {
                setTimeout(function() { if (overlay) beginRecognition(); }, 300);
            });
            return;
        }
        setStatus('fail', 'Nothing found', '');
        speak("I couldn't find that product. Try saying the name clearly in English or Tamil, or say show me vegetables to browse a category.", closeOverlay);
    }

    // ══════════════════════════════════════════════════════════════════
    //  CHECKOUT CONFIRMATION
    // ══════════════════════════════════════════════════════════════════
    function listenForCheckoutConfirmation() {
        if (!SpeechRec || !overlay) return;
        setStatus('cart', 'Say Yes to confirm or No to edit', '');

        var YES = ['yes','yeah','yep','yup','correct','right','confirm','proceed','checkout','place order','ok','okay','sure','go ahead','definitely','absolutely','please'];
        var NO  = ['no','nope','nah','cancel','remove','change','edit','something else','go back','back','never mind','nevermind','stop','wait'];

        var rec             = new SpeechRec();
        rec.lang            = 'en-US';
        rec.maxAlternatives = 5;

        rec.onresult = function(event) {
            var all = [];
            for (var r = 0; r < event.results.length; r++)
                for (var a = 0; a < event.results[r].length; a++)
                    all.push(event.results[r][a].transcript.toLowerCase());
            var combined = all.join(' ');
            if (YES.some(function(w) { return combined.indexOf(w) !== -1; })) {
                setStatus('ok', 'Proceeding to checkout!', '');
                speak('Awesome! Taking you to checkout.', function() { window.location.href = '/Cart/Checkout'; });
            } else if (NO.some(function(w) { return combined.indexOf(w) !== -1; })) {
                setStatus('retry', 'Going back to cart…', '');
                speak("Okay, let's go back to your cart.", function() { window.location.href = '/Cart/Index'; });
            } else {
                speak("Say yes to confirm, or no to edit your cart.", listenForCheckoutConfirmation);
            }
        };
        rec.onerror = function() {
            speak("Say yes to confirm, or no to go back.", listenForCheckoutConfirmation);
        };
        try { rec.start(); } catch(e) {}
    }

    window.startVoice = startListening;

})();
