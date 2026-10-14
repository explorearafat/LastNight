import React from "react";
import {
  IconAndroid,
  IconBeacon,
  IconBoatSmall,
  IconBox,
  IconBrick,
  IconClipboard,
  IconCloudStorm,
  IconDna,
  IconDownload,
  IconGithub,
  IconGlobe,
  IconLighthouseSmall,
  IconOffline,
  IconPersonBoat,
  IconSearch,
  IconSharkFin2,
  IconStory,
  IconSun,
  IconSurvival,
} from "./icons/icons";

// Direct-download path: put LastLight.apk at website/public/releases/LastLight.apk
// so Vite copies it to dist/releases/ and Vercel serves it with Content-Disposition: attachment.
const DIRECT_APK = "/releases/LastLight.apk";
const GITHUB_RELEASE_APK = "https://github.com/explorearafat/LastNight/releases/download/v0.5.0/LastLight.apk";
const FALLBACK_URL = "https://github.com/explorearafat/LastNight/releases";
const RELEASE_TAG = "v0.5.0";

type ApkTarget = { href: string; download: boolean; label: string };

function useApkTarget(): ApkTarget {
  // Prefer the direct APK if we can HEAD it; otherwise fall back based on build-time flag.
  // We use a cheap HEAD probe so the button degrades gracefully when APK is not yet published.
  const [target, setTarget] = React.useState<ApkTarget>({
    // While probing, show "Download APK" but still open the modal if APK not yet reachable
    href: FALLBACK_URL,
    download: false,
    label: "View releases",
  });

  React.useEffect(() => {
    let cancelled = false;
    async function probe(url: string): Promise<boolean> {
      try {
        const res = await fetch(url, { method: "HEAD", cache: "no-store" });
        // Some hosts (GitHub) don't allow HEAD cross-origin; treat any non-404 as "exists"
        return res.ok || res.status === 200 || res.status === 204 || res.status === 304;
      } catch {
        return false;
      }
    }
    (async () => {
      if (await probe(DIRECT_APK)) {
        if (!cancelled) setTarget({ href: DIRECT_APK, download: true, label: "v0.5 APK" });
        return;
      }
      if (await probe(GITHUB_RELEASE_APK)) {
        if (!cancelled) setTarget({ href: GITHUB_RELEASE_APK, download: true, label: "v0.5 APK" });
        return;
      }
      if (!cancelled) setTarget({ href: FALLBACK_URL, download: false, label: "View releases" });
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return target;
}

export default function App() {
  const [menuOpen, setMenuOpen] = React.useState(false);
  const [toast, setToast] = React.useState<string | null>(null);
  const [showComingSoon, setShowComingSoon] = React.useState(false);
  const apkTarget = useApkTarget();

  function showToast(msg: string) {
    setToast(msg);
    window.setTimeout(() => setToast(null), 2200);
  }

  async function onCopy() {
    const url = apkTarget.href;
    try {
      await navigator.clipboard.writeText(url);
      showToast("Link copied");
    } catch {
      showToast(url);
    }
  }

  function handleDownloadClick(e: React.MouseEvent<HTMLAnchorElement>) {
    if (!apkTarget.download) {
      e.preventDefault();
      setShowComingSoon(true);
    }
    // When apkTarget.download is true, let the browser handle the <a download> navigation
  }

  return (
    <>
      <nav className="nav">
        <div className="nav-inner">
          <a className="brand" href="#">
            <span className="brand-dot" aria-hidden />
            Last Light
          </a>
          <div className="nav-links" aria-label="Primary">
            <a href="#overview" className="active">
              Overview
            </a>
            <a href="#how-to-play">How to Play</a>
            <a href="#features">Docs</a>
            <a href="#download">Download</a>
            <a href="#creator">Creator</a>
          </div>
          <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
            <a className="nav-cta" href="#download">
              Download for Android
            </a>
            <button
              className="menu-btn"
              aria-label={menuOpen ? "Close menu" : "Open menu"}
              aria-expanded={menuOpen}
              onClick={() => setMenuOpen((v) => !v)}
            >
              <span aria-hidden style={{ fontSize: 16, lineHeight: 1 }}>
                {menuOpen ? "×" : "≡"}
              </span>
            </button>
          </div>
        </div>
        {menuOpen ? (
          <div className="mobile-panel show">
            <a href="#overview" onClick={() => setMenuOpen(false)}>
              Overview
            </a>
            <a href="#how-to-play" onClick={() => setMenuOpen(false)}>
              How to Play
            </a>
            <a href="#features" onClick={() => setMenuOpen(false)}>
              Docs
            </a>
            <a href="#download" onClick={() => setMenuOpen(false)}>
              Download
            </a>
            <a href="#creator" onClick={() => setMenuOpen(false)}>
              Creator
            </a>
          </div>
        ) : null}
      </nav>

      <section className="hero" id="overview">
        <div className="hero-inner">
          <div>
            <h1>
              <span>Keep the</span>
              <span>light alive.</span>
            </h1>
            <p>A realistic lighthouse survival game for Android. Survive the storm. Uncover the island&apos;s secret.</p>
            <div className="hero-actions">
              <a
                className="btn btn-primary"
                href={apkTarget.href}
                download={apkTarget.download ? "LastLight.apk" : undefined}
                target={apkTarget.download ? undefined : "_blank"}
                rel={apkTarget.download ? undefined : "noreferrer"}
                onClick={handleDownloadClick}
              >
                Download APK
              </a>
              <button className="btn btn-ghost" type="button" onClick={() => document.getElementById("gallery")?.scrollIntoView({ behavior: "smooth" })}>
                <IconPlay style={{ width: 14, height: 14 }} />
                Watch Trailer
              </button>
            </div>
          </div>
          <div className="hero-image" aria-hidden>
            {/* Lighthouse hero — replace with real screenshot when available */}
          </div>
        </div>
        <div className="hero-badges">
          <span className="badge">
            <IconAndroid style={{ width: 14, height: 14 }} />
            Android
          </span>
          <span className="badge">
            <IconOffline style={{ width: 14, height: 14 }} />
            Offline
          </span>
          <span className="badge">
            <IconSurvival style={{ width: 14, height: 14 }} />
            Survival
          </span>
          <span className="badge">
            <IconStory style={{ width: 14, height: 14 }} />
            Story-driven
          </span>
        </div>
      </section>

      <section className="section">
        <div className="about-grid">
          <div>
            <h2>About the Game</h2>
            <div className="about-text">
              <p>
                A realistic lighthouse survival game for Android. Survive the storm. Uncover the island&apos;s secret. This
                requires storm toma mn&apos;n protected light scannies in the keepe.
              </p>
              <p>
                The core gameplay loop control in the adrinire storme mechanics and mechanics, include end tramstormis
                anpinoce storm conceese and boat crosscanes.
              </p>
            </div>
          </div>
          <div className="about-image" aria-hidden />
        </div>
        <div className="about-full">
          <p>
            Paragraph 2 is realistic inomitis unique unique. Thc island&apos;s historcune, history, artifacts, artifacts,
            siscluded houses, vistors and unscawal shats, thow,cend hidden lore, and astors to siind trimns count and
            ooncreant.
          </p>
          <p>
            Paragraph 3 is the complex, multi-layered resource management system and survival challenge in the complexed
            resource and the survival commines area with advanced crafting, and statility, including advanced crafting.
          </p>
        </div>
      </section>

      <section className="section" id="how-to-play">
        <h2>How to Play</h2>
        <div className="how-grid" style={{ marginTop: 18 }}>
          <div className="how-step">
            <div className="how-num">1</div>
            <div className="how-icon" aria-hidden>
              <IconSun style={{ width: 36, height: 36 }} />
            </div>
            <h4>Explore by day</h4>
            <p>Explore the sxis in the crispers by day.</p>
          </div>
          <div className="how-step">
            <div className="how-num">2</div>
            <div className="how-icon" aria-hidden>
              <IconClipboard style={{ width: 36, height: 36 }} />
            </div>
            <h4>Gather supplies</h4>
            <p>Gather cooncoonates and gather supplies.</p>
          </div>
          <div className="how-step">
            <div className="how-num">3</div>
            <div className="how-icon" aria-hidden>
              <IconBoatSmall style={{ width: 36, height: 36 }} />
            </div>
            <h4>Cross by boat</h4>
            <p>Cross by boat hoas by sinto cormosi.</p>
          </div>
          <div className="how-step">
            <div className="how-num">4</div>
            <div className="how-icon" aria-hidden>
              <IconLighthouseSmall style={{ width: 36, height: 36 }} />
            </div>
            <h4>Protect the light at night</h4>
            <p>Protect the light at night.</p>
          </div>
        </div>
      </section>

      <section className="section" id="features">
        <h2>Features</h2>
        <div className="features-grid" style={{ marginTop: 18 }}>
          <div className="feat">
            <span className="feat-icon" aria-hidden>
              <IconCloudStorm style={{ width: 28, height: 28 }} />
            </span>
            <div>
              <h4>Adaptive Storm</h4>
              <p>Adaptive tigonlight is adaptive storm and stresed chronist ormament process wayamds the irrope.</p>
            </div>
          </div>
          <div className="feat">
            <span className="feat-icon" aria-hidden>
              <IconDna style={{ width: 28, height: 28 }} />
            </span>
            <div>
              <h4>Permanent Damage</h4>
              <p>Permanent Oonage permanent damage, vasting for imosists Demo conscints intost narinsg and comminat damage.</p>
            </div>
          </div>
          <div className="feat">
            <span className="feat-icon" aria-hidden>
              <IconPersonBoat style={{ width: 28, height: 28 }} />
            </span>
            <div>
              <h4>Boat Crossing</h4>
              <p>Boat ercaling tomsntens olark cricering around satpuwind onted orortalar instarces.</p>
            </div>
          </div>
          <div className="feat">
            <span className="feat-icon" aria-hidden>
              <IconSharkFin2 style={{ width: 28, height: 28 }} />
            </span>
            <div>
              <h4>Shark Waters</h4>
              <p>Enare texcoistic waters sotate liner, sestincs varse and explared taminte a tsits of hernociinc odoisons and oircens suits to undrastwat fuhns to Explore.</p>
            </div>
          </div>
          <div className="feat">
            <span className="feat-icon" aria-hidden>
              <IconRuins style={{ width: 28, height: 28 }} />
            </span>
            <div>
              <h4>Ruins to Explore</h4>
              <p>Ruins to Explore an enciosment problems.</p>
            </div>
          </div>
          <div className="feat">
            <span className="feat-icon" aria-hidden>
              <IconRunner style={{ width: 28, height: 28 }} />
            </span>
            <div>
              <h4>Realistic Movement</h4>
              <p>Realistic soorions pomproment odronage, semonros nassing and consinent darrabe.</p>
            </div>
          </div>
        </div>
      </section>

      <section className="section" id="gallery" style={{ display: "none" }} aria-hidden>
        <h2>Gallery</h2>
        <p className="section-sub">Blender kit previews — real lighting is baked GI and the beacon.</p>
      </section>

      <section className="section" id="download">
        <div className="docs-download">
          <div className="docs">
            <h3>Documentation</h3>
            <nav className="docs-links" aria-label="Docs">
              <a href="#features">Game Guide</a>
              <a href="#how-to-play">Controls</a>
              <a href="#features">Story and Lore</a>
              <a href="#download">FAQ</a>
              <a href="https://github.com/explorearafat/LastNight/blob/main/PROGRESS.md" target="_blank" rel="noreferrer">
                Patch Notes
              </a>
              <a href="#download">System Requirements</a>
            </nav>
          </div>
          <div className="dl">
            <h3>Download</h3>
            <div className="dl-card">
              <div className="dl-icon" aria-hidden>
                <IconAndroid style={{ width: 28, height: 28 }} />
              </div>
              <div className="meta">Version: 1.3 | 338 MB</div>
              <div className="meta" style={{ marginBottom: 14 }}>
                System requirements: 1833520
              </div>
              <a
                className="btn btn-primary btn-full"
                href={apkTarget.href}
                download={apkTarget.download ? "LastLight.apk" : undefined}
                target={apkTarget.download ? undefined : "_blank"}
                rel={apkTarget.download ? undefined : "noreferrer"}
                onClick={handleDownloadClick}
              >
                <IconDownload style={{ width: 14, height: 14 }} />
                Download APK — {apkTarget.label}
              </a>
              <div style={{ marginTop: 10 }}>
                <button
                  className="btn btn-ghost"
                  type="button"
                  onClick={onCopy}
                  style={{ fontSize: 11, padding: "7px 12px", width: "100%", justifyContent: "center" }}
                >
                  Copy link
                </button>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="creator" id="creator">
        <h3>Meet the Creator</h3>
        <div className="creator-avatar" aria-hidden>
          <svg width="28" height="28" viewBox="0 0 24 24" fill="currentColor">
            <circle cx="12" cy="8" r="4" />
            <path d="M5 20c0-3.5 2.8-6 7-6s7 2.5 7 6" />
          </svg>
        </div>
        <p className="creator-name">Name name</p>
        <p className="creator-bio">Lorem ipsum dolor sit amet elestuer adipiscing elit, sed diam elestue tio.</p>
        <div className="creator-social" aria-label="Social links">
          <a href="https://github.com/explorearafat" target="_blank" rel="noreferrer" aria-label="GitHub">
            <IconGithub style={{ width: 12, height: 12 }} />
          </a>
          <a href="#" aria-label="Globe">
            <IconGlobe style={{ width: 12, height: 12 }} />
          </a>
          <a href="#" aria-label="Search">
            <IconSearch style={{ width: 12, height: 12 }} />
          </a>
          <a href="#" aria-label="Box">
            <IconBox style={{ width: 12, height: 12 }} />
          </a>
        </div>
      </section>

      <section className="section" style={{ paddingTop: 28 }}>
        <h2>How to Play — install</h2>
        <div style={{ display: "grid", gap: 10, marginTop: 14 }}>
          <div style={{ display: "flex", gap: 12, background: "var(--card)", border: "1px solid var(--line)", borderRadius: 10, padding: 14 }}>
            <span style={{ minWidth: 28, height: 28, borderRadius: "50%", display: "grid", placeItems: "center", background: "var(--accent)", color: "#fff", fontWeight: 700, fontSize: 12 }}>1</span>
            <div>
              <b style={{ fontSize: 12 }}>Download the APK</b>
              <p style={{ margin: "4px 0 0", fontSize: 11, color: "var(--muted)", lineHeight: 1.5 }}>
                Tap <strong>Download APK</strong> above on your Android phone. The file lands in <code>Downloads/LastLight.apk</code>.
              </p>
            </div>
          </div>
          <div style={{ display: "flex", gap: 12, background: "var(--card)", border: "1px solid var(--line)", borderRadius: 10, padding: 14 }}>
            <span style={{ minWidth: 28, height: 28, borderRadius: "50%", display: "grid", placeItems: "center", background: "var(--accent)", color: "#fff", fontWeight: 700, fontSize: 12 }}>2</span>
            <div>
              <b style={{ fontSize: 12 }}>Allow unknown apps</b>
              <p style={{ margin: "4px 0 0", fontSize: 11, color: "var(--muted)", lineHeight: 1.5 }}>
                <code>Settings → Apps → Special access → Install unknown apps → Chrome or Files → Allow</code>. One time only.
              </p>
            </div>
          </div>
          <div style={{ display: "flex", gap: 12, background: "var(--card)", border: "1px solid var(--line)", borderRadius: 10, padding: 14 }}>
            <span style={{ minWidth: 28, height: 28, borderRadius: "50%", display: "grid", placeItems: "center", background: "var(--accent)", color: "#fff", fontWeight: 700, fontSize: 12 }}>3</span>
            <div>
              <b style={{ fontSize: 12 }}>Install and play</b>
              <p style={{ margin: "4px 0 0", fontSize: 11, color: "var(--muted)", lineHeight: 1.5 }}>
                Open the APK → <strong>Install</strong> → <strong>Open</strong>. Ferry fuel by boat before dusk — keep the light on.
              </p>
            </div>
          </div>
        </div>
      </section>

      <footer className="footer">
        <span style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span style={{ width: 8, height: 8, borderRadius: "50%", background: "var(--accent)", display: "inline-block" }} aria-hidden />
          Last Light
        </span>
        <span className="footer-links">
          <a href="#overview">Overview</a>
          <a href="#how-to-play">How to Play</a>
          <a href="#features">Docs</a>
          <a href="#download">Download</a>
          <a href="#creator">Creator</a>
        </span>
        <span>© Copyright at lightxx.com</span>
      </footer>

      {showComingSoon ? (
        <div
          role="dialog"
          aria-modal="true"
          aria-label="APK coming soon"
          onClick={() => setShowComingSoon(false)}
          style={{
            position: "fixed",
            inset: 0,
            background: "rgba(26,30,34,0.52)",
            display: "grid",
            placeItems: "center",
            padding: 20,
            zIndex: 80,
          }}
        >
          <div
            onClick={(e) => e.stopPropagation()}
            style={{
              background: "var(--card)",
              border: "1px solid var(--line)",
              borderRadius: 14,
              padding: 22,
              maxWidth: 420,
              width: "100%",
              boxShadow: "0 16px 40px rgba(0,0,0,0.12)",
            }}
          >
            <h3 style={{ fontFamily: '"Fraunces", serif', fontSize: 18, margin: "0 0 8px" }}>APK coming soon</h3>
            <p style={{ fontSize: 12.5, lineHeight: 1.55, color: "var(--muted)", margin: "0 0 14px" }}>
              The Android build isn&apos;t published yet. The first APK will appear on{" "}
              <a href={FALLBACK_URL} target="_blank" rel="noreferrer" style={{ textDecoration: "underline", textUnderlineOffset: 3 }}>
                GitHub Releases ({RELEASE_TAG})
              </a>{" "}
              once the Unity license is configured for CI. You can also build locally:{" "}
              <code>File → Build Settings → Android → Build</code> in Unity 6000.0.38f1.
            </p>
            <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", flexWrap: "wrap" }}>
              <button className="btn btn-ghost" type="button" onClick={() => setShowComingSoon(false)}>
                Close
              </button>
              <a className="btn btn-primary" href={FALLBACK_URL} target="_blank" rel="noreferrer">
                Go to Releases
              </a>
            </div>
          </div>
        </div>
      ) : null}

      {toast ? (
        <div className="toast show" role="status" aria-live="polite">
          {toast}
        </div>
      ) : null}
    </>
  );
}
