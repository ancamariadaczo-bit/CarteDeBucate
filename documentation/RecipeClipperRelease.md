# Recipe Clipper – Release checklist

1. Make changes and test locally, from Chrome://Extensions with Reload.
2. Increase `version` in `manifest.json`.
3. Update Privacy Policy only if data usage or permissions changed.
The Privacy Policy is stored in docs/index.html and published through GitHub Pages. Any privacy-related changes should be made in this file. After committing and pushing the changes, GitHub Pages updates automatically and the Privacy Policy URL remains the same.
4. Commit and push to GitHub.
5. Create a new extension ZIP.
6. Chrome Web Store → Package → Upload New Package.
7. Update Store listing / Privacy practices if necessary.
8. Save draft → Submit for Review.
9. After publishing, test the Web Store version.