# Agent-kit kişi paketi (speed-starts)

Aynı `main`'de iki kişi kitle çalışıyor: **biz** (Pundun, UI/meta) ve **malik** (karakter mekaniği). Hook ayarı, agent tanımları,
kilitler ve oturum kuralları kişiye özel ve **git'e girmez**; şablonları burada.

| Dosya | Kurulunca | Ne |
|---|---|---|
| `<kisi>.config.local.psd1` | `agent-kit.config.local.psd1` | Ortak `agent-kit.config.psd1`'in anahtarlarını ezer: kilitli yollar, güvenlik dosyaları, DURUM/task klasörü, kopya yeri |
| `<kisi>.CLAUDE.local.md` | `CLAUDE.local.md` | Oturuma kişinin kural dosyasını yükler (`docs/CALISMA-KURALLARI*.md`) |
| `settings.json` / `malik.settings.json` | `.claude/settings.json` | Hook'lar (Malik'te MCP `refresh_unity` engeli kapalı: prefab'ı sık değiştiriyor) |
| `agents/*.md` | `.claude/agents/` | Alt agent tanımları |

## Malik — kurulum (bir kez, ~2 dk)
1. `git pull` (kit dosyaları `scripts/`, `prompts/`, `agent-kit/` ile gelir).
2. Proje kökünde: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/kit-kur-yerel.ps1 -Kisi malik`
   (önce görmek için `-Kuru`). Var olan dosyanın üstüne yazmaz.
3. `agent-kit.config.local.psd1` → `KopyaKoku`'yu doldur (doğrulama kopyalarının klasörü, depo dışında; ör. `D:\Pundun`).
4. Claude Code'u proje klasöründe **yeniden aç** (hook'lar ve `CLAUDE.local.md` oturum başında yüklenir). İlk mesaj:
   > `docs/DURUM-malik.md`'yi ve `docs/CALISMA-KURALLARI-malik.md`'yi oku; agent-kit düzeniyle devam. Hook'ları dene: bir `.prefab`'a
   > Write ve `git add -A` engellenmeli, `Scripts/UI/` altına Write engellenmeli, `Scripts/Physical/` serbest olmalı.
5. İsteğe bağlı — **pano** yan paneli: kit klasörü (`D:\Pundun\agent-kit`, ayrı depo) o makinede de olmalı; sonra
   `claude plugin marketplace add <kit-klasörü>` ve `claude plugin install pano@agent-kit -s local`.

## Ne değişir, ne değişmez (Malik için)
- `CLAUDE.md` senin devir notun olarak kalır, güncellemeye devam edersin (sende kilitli değil).
- UI/meta tarafının dosyaları sende kilitli (yazılmaz, okunur). Mekaniğin public API'sini değiştirmeden önce Pundun'a haber.
- Commit'ler yolla sınırlı (`git commit -F msg -- <yollar>`), `git add -A` engelli; push'u yine sen yaparsın.
- Kendi durumun `docs/DURUM-malik.md` (ilk satır `MOD: hızlı`), task'ların `docs/tasks-malik/`.

## Biz — başka makinede
`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/kit-kur-yerel.ps1 -Kisi biz` + `claude plugin install pano@agent-kit -s local`.
