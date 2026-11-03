---
id: T-095
title: Lisans envanteri - build'e giren ucuncu taraf varliklar (font, UI sanati, model, animasyon, ses, eklenti), kaynak ve risk
status: todo
owner: denetci-agent
depends_on: []
owns_paths:
  - "reports/lisans-envanteri-2026-11-03.md"
touches_scene: false
branch:
commits: []
tests:
  - "gerekmiyor: rapor, kod yok"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Yol haritasi listesi 00-06 "Telif/lisans kontrolu" acik (docs/YOL-HARITASI.md ~100. satir: GUI PRO Kit, CasualFantasy referanslari, 25 yer).
Build'e giren her ucuncu taraf varligin envanterini cikar: Build Settings sahneleri (Home, Race) + Resources + bu sahnelerin referans
verdigi prefab/doku/font/model/animasyon/ses (`.meta` guid'leri ve `AssetDatabase` yerine dosya taramasiyla: sahne/prefab YAML'indaki
guid -> .meta -> yol). Her kalem icin: klasor/paket, ne oldugu, nerede kullaniliyor, kaynak (Asset Store paketi, Mixamo, OFL font,
kullanicinin kendi sanati, uretilmis yer tutucu, bilinmiyor), lisansin projede kaniti (LICENSE/readme dosyasi, paket adi) ve risk
(yok / dusuk / incelenmeli / yuksek). Ozellikle bak: (1) referans oyuna ait olabilecek her sey - urun adi (PlayerSettings productName /
companyName "Speed Stars" ise ticari marka riski), logo, ekran goruntusunden kirpilmis UI sanati, oyunun isimleri; (2) kaynagi belirsiz
UI sanati (docs/eksik-ui, docs/gdd/referans-ekranlar build'e girmiyorsa "girmiyor" diye yaz); (3) fontlar (LegacyRuntime, Kanit OFL);
(4) emote animasyonlari (Mixamo) ve gorunen adlari; (5) DOTween/Zenject/MCP lisanslari (build'e giriyor mu). Hukuki hukum verme:
"kanit var / kanit yok / incelenmeli" yaz. Hicbir dosyayi degistirme; yalniz rapor + bu task dosyasi.

## Kabul kriterleri
- [ ] reports/lisans-envanteri-2026-11-03.md: tablo (kalem, yol, kullanim, kaynak, kanit, risk) + en ustte "incelenmeli / yuksek" ozeti
- [ ] Build'e girmeyen klasorler ayri listede ("build'e girmiyor")
- [ ] Hicbir asset/kod degismedi

## Kapsam disi
- Varlik degistirme/silme, hukuki gorus

## Calisma notu
