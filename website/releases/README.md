# Releases

Place your built APK here as `LastLight.apk`.

Built via GitHub Actions: push a tag `v*` (e.g. `v0.5.0`) → workflow builds Android APK and creates a GitHub Release.
The website Download button points to the latest Release asset; update `APK_URL` in `website/src/App.tsx` to the Release URL after the first build.

Local build: open `Assets/Scenes/Island.unity` in Unity 6000.0.38f1 → File → Build Settings → Android → Build.
