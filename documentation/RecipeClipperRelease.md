# Recipe Clipper – Release checklist

1. Make changes and test locally, from Chrome://Extensions with Reload. Keep `API_BASE_URL` in `config/apiConfig.js` set to `https://localhost:7080` and keep `manifest.json` with the single matching host permission `https://localhost:7080/*` during local development.
2. Verify that the extension ID from `chrome://extensions` matches both `Cors:AllowedOrigins` and `ChromeExtension:AuthenticationRedirectUrl` in the Web configuration.
3. Increase `version` in `manifest.json`.
4. Update Privacy Policy only if data usage or permissions changed.
The Privacy Policy is stored in docs/index.html and published through GitHub Pages. Any privacy-related changes should be made in this file. After committing and pushing the changes, GitHub Pages updates automatically and the Privacy Policy URL remains the same.
5. Commit and push to GitHub.
6. Set `API_BASE_URL` in `config/apiConfig.js` to `https://cartedebucate.onrender.com` and replace the single host permission in `manifest.json` with `https://cartedebucate.onrender.com/*`. Do not leave either localhost permission in the release manifest.
7. Create a new extension ZIP. Source changes do not update `Recipe Clipper 1.0.0.zip` automatically, so never reuse the old archive without rebuilding it.
8. Restore `API_BASE_URL` to `https://localhost:7080` and the single manifest host permission to `https://localhost:7080/*` in the source tree for local development.
9. Chrome Web Store → Package → Upload New Package.
10. Update Store listing / Privacy practices if necessary.
11. Save draft → Submit for Review.
12. After publishing, test the Web Store version.
