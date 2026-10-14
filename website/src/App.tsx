import React from "react";
import {
  IconArrow,
  IconBeacon,
  IconBoat,
  IconBrick,
  IconCopy,
  IconDownload,
  IconGithub,
  IconSharkFin,
  IconTree,
  IconWaves,
} from "./icons/icons";

const APK_URL = "";
const FALLBACK_URL = "https://github.com/explorearafat/LastNight/releases";

export default function App() {
  const [menuOpen, setMenuOpen] = React.useState(false);
  const [toast, setToast] = React.useState<string | null>(null);

  const apkHref = APK_URL || FALLBACK_URL;
  const apkLabel = APK_URL ? "v0.5 APK" : "View releases";
  const apkDownload = Boolean(APK_URL);

  function showToast(msg: string) {
    setToast(msg);
    window.setTimeout(() => setToast(null), 2200);
  }

  async function onCopy() {
    const url = APK_URL || FALLBACK_URL;
    try {
      await navigator.clipboard.writeText(url);
      showToast("Link copied");
    } catch {
      showToast(url);
    }
  }

  return (
    <>
      <nav className="nav">
        <div className="nav-inner">
          <a className="brand" href="#">
            <span className="brand-mark" aria-hidden>
              <IconBeacon style={{ width: 18, height: 18 }} />
            </span>
            LAST LIGHT
          </a>
          <div className="nav-links" aria-label="Primary">
            <a href="#features">Features</a>
            <a href="#gallery">Gallery</a>
            <a href="#download">Download</a>
            <a href="https://github.com/explorearafat/LastNight" target="_blank" rel="noreferrer">
              GitHub
            </a>
          </div>
          <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
            <a className="nav-cta" href="#download">
              Download APK
            </a>
            <button
              className="menu-btn"
              aria-label={menuOpen ? "Close menu" : "Open menu"}
              aria-expanded={menuOpen}
              onClick={() => setMenuOpen((v) => !v)}
            >
              <span aria-hidden style={{ fontSize: 16, lineHeight: 1 }}>{menuOpen ? "×" : "≡"}</span>
            </button>
          </div>
        </div>
        {menuOpen ? (
          <div className="mobile-panel">
            <a href="#features" onClick={() => setMenuOpen(false)}>
              Features
            </a>
            <a href="#gallery" onClick={() => setMenuOpen(false)}>
              Gallery
            </a>
            <a href="#download" onClick={() => setMenuOpen(false)}>
              Download
            </a>
            <a href="https://github.com/explorearafat/LastNight" target="_blank" rel="noreferrer">
              GitHub
            </a>
          </div>
        ) : null}
      </nav>

      <header className="hero">
        <div className="hero-bg" aria-hidden />
        <div className="hero-inner">
          <div>
            <div className="kicker">Unity 6 URP — Android — Day 5 Archipelago</div>
            <h1>
              Keep the <i>light</i> on.
            </h1>
            <p>
              A lighthouse survival game. You are <strong>Elias</strong>, alone on a storm-hit archipelago. By day you
              ferry fuel by boat through shark waters. By night the light must not fail.
            </p>
            <div className="actions">
              <a className="btn btn-primary" href="#download">
                <IconDownload style={{ width: 16, height: 16 }} />
                Download APK
              </a>
              <a className="btn btn-ghost" href="https://github.com/explorearafat/LastNight" target="_blank" rel="noreferrer">
                <IconGithub style={{ width: 16, height: 16 }} />
                View on GitHub
                <IconArrow style={{ width: 14, height: 14 }} />
              </a>
            </div>
            <div className="hero-note">
              <span className="dot" aria-hidden />
              <span>
                Latest: <strong style={{ color: "var(--text)" }}>Day 5 CP2 — archipelago + boat + sharks</strong> · Vulkan · 30 FPS min · ~14K tris
              </span>
            </div>
          </div>

          <div className="hero-card">
            <div className="hero-visual" aria-hidden>
              <span className="sweep" />
              <span className="beacon" />
            </div>
            <div className="hero-meta">
              <div className="pills">
                <span className="pill">
                  <strong>Islet</strong> r75 at -520 + 260 m channel
                </span>
                <span className="pill">
                  <strong>750 m</strong> main island
                </span>
                <span className="pill">~75 s crossing</span>
              </div>
              <div className="stats">
                <div className="stat">
                  <b>27</b>
                  <span>FBX · 1.4 MB</span>
                </div>
                <div className="stat">
                  <b>3</b>
                  <span>Sharks</span>
                </div>
                <div className="stat">
                  <b>18 kg</b>
                  <span>Boat ferry</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </header>

      <section className="section" id="features">
        <div className="section-head">
          <h2>Built for the storm.</h2>
          <p>No markers. No health bars. The wind, the hull, and the dark tell you everything.</p>
        </div>
        <div className="grid-3">
          <article className="card">
            <span className="badge badge-new">New — Islet</span>
            <div className="card-icon" aria-hidden>
              <IconBeacon style={{ width: 18, height: 18 }} />
            </div>
            <h3>Archipelago survival</h3>
            <p>
              Lighthouse on its own islet (r75). 260 m shark channel to cross. Main island holds all resources —
              castle, flooded bunker, cannery, cave. Ferry before dusk.
            </p>
          </article>
          <article className="card">
            <span className="badge badge-new">New — Boat</span>
            <div className="card-icon" aria-hidden>
              <IconBoat style={{ width: 18, height: 18 }} />
            </div>
            <h3>Boat crossing</h3>
            <p>
              Fake-buoyancy rowboat — joystick steering, board and unboard, wake and bob. About 75 seconds in calm,
              storm scales damage. Sinks if you push it.
            </p>
          </article>
          <article className="card">
            <span className="badge badge-new">New — Sharks</span>
            <div className="card-icon" aria-hidden>
              <IconSharkFin style={{ width: 18, height: 18 }} />
            </div>
            <h3>Shark channel</h3>
            <p>
              Three sharks (SharkProfile). Patrol, circle, stalk, attack, repel. Fins stay visible. Splash and blood
              attract, flare repels. Worse at night and in storm.
            </p>
          </article>
          <article className="card">
            <div className="card-icon" aria-hidden>
              <IconBrick style={{ width: 18, height: 18 }} />
            </div>
            <h3>Brick ruins</h3>
            <p>Modular brick kit — 37 meshes, 476 tris. Collapsed castle and half-flooded bunker, moss and algae in vertex color.</p>
          </article>
          <article className="card">
            <div className="card-icon" aria-hidden>
              <IconTree style={{ width: 18, height: 18 }} />
            </div>
            <h3>Scattered wild</h3>
            <p>Nature kit + scatter — pines, scrub, grass and fern cards, cliffs, driftwood. Zone-aware, channel-safe, instanced.</p>
          </article>
          <article className="card">
            <div className="card-icon" aria-hidden>
              <IconWaves style={{ width: 18, height: 18 }} />
            </div>
            <h3>Threatening water</h3>
            <p>Channel darkens with storm, 1.6× wave amp inside. Shore foam and depth tint on one draw.</p>
          </article>
          <article className="card">
            <div className="card-icon" aria-hidden>
              <IconWaves style={{ width: 18, height: 18 }} />
            </div>
            <h3>Adaptive storm</h3>
            <p>Learns your favourite crossing and where you hide. Next night it hits that weakness first — never the same twice.</p>
          </article>
          <article className="card">
            <div className="card-icon" aria-hidden>
              <IconBeacon style={{ width: 18, height: 18 }} />
            </div>
            <h3>Lore and hints</h3>
            <p>Eight notes across islet and ruins. Smoke and warm light pull you in — no markers, only smoke on the horizon.</p>
          </article>
          <article className="card">
            <div className="card-icon" aria-hidden>
              <IconWaves style={{ width: 18, height: 18 }} />
            </div>
            <h3>Mobile first</h3>
            <p>Vulkan, ASTC, baked GI, isStatic batching, LODs, occlusion. 10–14K visible tris, 42–75 draws, 30 FPS minimum.</p>
          </article>
        </div>
      </section>

      <section className="section" id="gallery">
        <div className="section-head">
          <h2>Gallery</h2>
          <p>Blender kit previews — EEVEE, not in-game URP. Real lighting is baked GI and the beacon.</p>
        </div>
        <div className="gallery">
          <div className="shot">
            <div className="shot-img">16 m lighthouse — white and red on its own islet</div>
            <div className="shot-body">
              <b>16 m lighthouse tower</b>
              <p>1696 tris — the light is the only thing between ships and rock.</p>
            </div>
          </div>
          <div className="shot">
            <div className="shot-img">Wooden boat — your lifeline across 260 m of dark water</div>
            <div className="shot-body">
              <b>Wooden boat — your lifeline</b>
              <p>Board at the dock, steer with joystick. The channel is 260 m of dark water.</p>
            </div>
          </div>
          <div className="shot">
            <div className="shot-img">Collapsed castle and flooded bunker — one brick kit</div>
            <div className="shot-body">
              <b>Collapsed castle and bunker</b>
              <p>One brick kit (37 meshes) — two ruins. Moss, cracks, rubble.</p>
            </div>
          </div>
          <div className="shot">
            <div className="shot-img">Wild main island — pines, scrub, cards, cliffs</div>
            <div className="shot-body">
              <b>Wild main island</b>
              <p>Pines, scrub, grass cards, cliffs — scattered and channel-safe.</p>
            </div>
          </div>
          <div className="shot">
            <div className="shot-img">Shark — fin above water, always visible</div>
            <div className="shot-body">
              <b>Shark — fin above water</b>
              <p>24 tris, three in the channel. Fins stay visible. Splash calls them in.</p>
            </div>
          </div>
          <div className="shot">
            <div className="shot-img">Archipelago — islet, channel, main island</div>
            <div className="shot-body">
              <b>Archipelago</b>
              <p>Lighthouse islet (r75) + 260 m shark channel + 750 m main island — ~14K visible tris.</p>
            </div>
          </div>
        </div>
      </section>

      <section className="section" id="download">
        <div className="download">
          <div>
            <h3>Download and install</h3>
            <p>
              APK works on Android 8 and later (API 24). No Play Store needed — sideload in one tap. Updates land here
              when a new release is pushed to GitHub.
            </p>
            <div className="dl-actions">
              <a className="btn btn-primary" href={apkHref} download={apkDownload ? "" : undefined} target={apkDownload ? undefined : "_blank"} rel={apkDownload ? undefined : "noreferrer"}>
                <IconDownload style={{ width: 16, height: 16 }} />
                Download APK — {apkLabel}
              </a>
              <button className="btn btn-ghost" type="button" onClick={onCopy}>
                <IconCopy style={{ width: 16, height: 16 }} />
                Copy link
              </button>
              <a className="btn btn-ghost" href="https://github.com/explorearafat/LastNight/releases" target="_blank" rel="noreferrer">
                <IconGithub style={{ width: 16, height: 16 }} />
                All releases
              </a>
            </div>
            <p style={{ marginTop: 14, fontSize: 12, color: "var(--muted-2)", lineHeight: "1.5" }}>
              On your phone: <strong style={{ color: "var(--muted)" }}>Settings → Security → Install unknown apps → Allow</strong>, then open the APK. Or: <code>adb install -r LastLight.apk</code>
            </p>
          </div>
          <div className="dl-meta">
            <div className="dl-row">
              <div>
                <b>v0.5 — Day 5 CP2</b>
                <br />
                <span>Archipelago + sharks · 1.4 MB FBX</span>
              </div>
              <span className="pill">Vulkan · ARM64</span>
            </div>
            <div className="dl-row">
              <div>
                <b>Requirements</b>
                <br />
                <span>Android 8+ · 1.5 GB RAM · about 80 MB</span>
              </div>
              <span className="pill" style={{ color: "var(--ok)", borderColor: "rgba(61,214,140,0.22)" }}>
                30 FPS min
              </span>
            </div>
            <div className="dl-row">
              <div>
                <b>How to play</b>
                <br />
                <span>Left joystick move · Right swipe look · E or Space to interact and board · Ferry fuel before dusk</span>
              </div>
              <span className="pill">No account</span>
            </div>
            <div style={{ display: "flex", gap: 12, alignItems: "center", marginTop: 4 }}>
              <div className="qr" aria-hidden>
                QR — scan
                <br />
                to download
              </div>
              <div style={{ fontSize: 12, color: "var(--muted)", lineHeight: "1.5" }}>
                Scan with your phone camera to open this page and download. The QR points to the same APK link.
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="section">
        <h2 style={{ fontFamily: '"Fraunces", serif', fontSize: 22, margin: "0 0 12px", letterSpacing: "-0.02em" }}>Install — step by step</h2>
        <div className="steps">
          <div className="step">
            <span className="step-num" aria-hidden>
              1
            </span>
            <div>
              <b>Download the APK</b>
              <p>
                Tap <strong>Download APK</strong> above on your Android phone. The file lands in <code>Downloads/LastLight.apk</code>.
              </p>
            </div>
          </div>
          <div className="step">
            <span className="step-num" aria-hidden>
              2
            </span>
            <div>
              <b>Allow unknown apps</b>
              <p>
                <code>Settings → Apps → Special access → Install unknown apps → Chrome or Files → Allow</code>. One time only.
              </p>
            </div>
          </div>
          <div className="step">
            <span className="step-num" aria-hidden>
              3
            </span>
            <div>
              <b>Install and play</b>
              <p>
                Open the APK → <strong>Install</strong> → <strong>Open</strong>. Ferry fuel by boat before dusk — keep the light on.
              </p>
            </div>
          </div>
          <div className="step">
            <span className="step-num" aria-hidden>
              4
            </span>
            <div>
              <b>Update</b>
              <p>
                Return here for new releases — install over the old APK, your save in <code>persistentDataPath/lastlight_save.json</code> is kept.
              </p>
            </div>
          </div>
        </div>
      </section>

      <section className="section" style={{ paddingTop: 0 }}>
        <div className="card" style={{ display: "flex", gap: 16, alignItems: "center", flexWrap: "wrap" }}>
          <div style={{ flex: "1 1 240px", minWidth: 240 }}>
            <h3 style={{ margin: "0 0 6px" }}>Building your own APK</h3>
            <p>
              Clone{" "}
              <a href="https://github.com/explorearafat/LastNight" target="_blank" rel="noreferrer" style={{ textDecoration: "underline", textUnderlineOffset: 3 }}>
                explorearafat/LastNight
              </a>{" "}
              in Unity 6000.0.38f1 with Android Build Support — <code>File → Build Settings → Android → Build and Run</code> over USB. See{" "}
              <code>README.md</code> and <code>PROGRESS.md → Manual steps</code>.
            </p>
          </div>
          <a className="btn btn-ghost" href="https://github.com/explorearafat/LastNight" target="_blank" rel="noreferrer">
            <IconGithub style={{ width: 16, height: 16 }} />
            GitHub
          </a>
        </div>
      </section>

      <footer className="footer">
        Last Light — a lighthouse survival game by{" "}
        <a href="https://github.com/explorearafat" target="_blank" rel="noreferrer">
          explorearafat
        </a>{" "}
        · Unity 6 URP ·{" "}
        <a href="https://github.com/explorearafat/LastNight" target="_blank" rel="noreferrer">
          Source
        </a>{" "}
        · Day 5 — Archipelago (islet + 260 m channel + main island)
      </footer>

      {toast ? (
        <div className="toast show" role="status" aria-live="polite">
          {toast}
        </div>
      ) : null}
    </>
  );
}
