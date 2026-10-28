---
id: T-000
title: Kisa baslik
status: todo
owner: gameplay-agent
depends_on: []
owns_paths:
  - "Assets/Game/Scripts/Ornek/**"
touches_scene: false
branch:
commits: []
tests: []
analytics: []
needs: []
started:
finished:
blocked_reason:
---

<!--
FORMAT KURALLARI (scripts/tur-sonu.ps1 bu bolumu makineyle okur - bozma):
- Frontmatter yalniz su bicimleri kullanir: `anahtar: deger`, `anahtar: [a, b]`, `anahtar: []`,
  blok liste (`  - deger`) ve needs icin harita listesi (asagida). Satir sonu yorumu (`# ...`) YAZMA.
- status YALNIZ su degerlerden biri:
    todo         henuz baslanmadi
    in-progress  agent calisiyor
    review       agent bitirdi, supervizor inceleyecek
    blocked      ilerleyemiyor (blocked_reason dolu olmali)
    merged       supervizor dogruladi ve ana dala gecirdi; KULLANICININ GOZUNU bekliyor
    done         kullanici "tamam" dedi (ya da gozle bakilacak bir yani yok ve supervizor kapatti)
    cancelled    vazgecildi (blocked_reason'a neden yaz)
  Aciklama, rapor, tarih status satirina YAZILMAZ; "## Calisma notu"na yazilir.
- owner: gameplay-agent | systems-agent | ui-agent | scene-integrator | test-agent | denetci-agent | supervisor
- tests: bu task'in test dosyalari (proje kokune gore yol) YA DA tek eleman "gerekmiyor: <gerekce>"
- analytics: bu task'in yayinladigi olay adlari (katalogdaki adla) YA DA tek eleman "yok: <gerekce>"
- commits: merged olurken supervizor ana daldaki hash'leri yazar
- needs: acik/kapali baglantilar. Her madde uc alan:
    needs:
      - tip: SCENE            SCENE | WIRING | ASSET | DECISION
        durum: open           open | closed
        not: "Resources/UI/X.prefab icine Y butonu, View.button alanina bagla"
  Kapatan kisi durum'u closed yapar ve not'un sonuna " -> <commit/aciklama>" ekler.
  Govdeye "NEEDS_SCENE: yok" gibi satir YAZMA; ihtiyac yoksa needs: [] kalir.
- Tur sonu kapisi: hicbir task'ta durum: open WIRING/SCENE kalmadan tur kapanmaz.
-->

## Hedef
Ne yapilacak, tek paragraf. Mevcut kodu referans al; eski tasarim belgesine atif verme.

## Kabul kriterleri
Her madde dogrulanabilir olmali. "Iyi hissettirsin" gecersiz.

- [ ] <davranis: X girdisiyle Y sonucu uretir>
- [ ] Testi var ve sabit denge sayisi assert etmiyor (iliski assert eder ya da sayiyi config asset'inden okur)
- [ ] needs listesinde open WIRING/SCENE yok
- [ ] Oyunda ulasilabilir (sadece derlenmis olmasi yetmez)
- [ ] Onemli olaysa analytics alaninda olay adi var (CLAUDE.md analytics kurali)

## Geri bildirim
Etkilesim ekliyorsa (vurus, kirilma, satin alma, acilma, hata) bes satir doldurulur;
"gerekmiyor" gecerli ama YAZILMIS olmali. Etkilesim yoksa bolumu sil.

GORSEL  : <renk flasi / scale punch / partikul / iz>
SES     : <hangi Sfx enum degeri>
DOKUNSAL: <hangi HapticTone / yok>
ZAMAN   : <hit-stop var mi, kac ms / yok>
KAMERA  : <shake yogunlugu / yok>

## Kapsam disi
- <bu task'ta YAPILMAYACAK sey>

## Calisma notu
(Agent doldurur: ne yapti, nerede zorlandi, ne birakti - 5 satir)
