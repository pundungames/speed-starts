---
id: T-119
title: Ayak yine zemine giriyor (T-117 geri getirdi), on ayak yukari, duzensiz tek tus atlamasi, zincirin kalani
status: review
owner: gameplay-agent
depends_on: [T-118]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [457f6f62, 2c05512a, 85632de2, c56b10a2, 7980816b]
tests:
  - "gerekmiyor: kullanici 05.11 'test etmeden yap, en son ben bakarim'; gorsel, derleme + kullanicinin gozu"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici: "sen devam et, soylicem ben bakip". Mevcut olcum verisi `D:\Pundun\form-probe\t117f\` (T-118'den once) okunarak cikan kalanlar. Unity, test, olcum yok.

## Isler
1. **Zemin geri geldi (T-115'te temizdi, T-117 bozdu):** en dusuk ayak ucu, sinir +0.5 cm:
   | senaryo | ayak ucu | t | faz | uyluk / diz |
   |---|---|---|---|---|
   | hopRough | −2.7 (11 kare) | 2.90 | Mid | 4.9 / 139 |
   | holdOne | −1.4 | 2.27 | Rest | 16.1 / 159 |
   | human | −0.9 (4 kare) | 3.63 | Push | 12.7 / 119 |
   | tap012 | −0.8 | 4.88 | Reach | 31.6 / 100 |
   | swap | −0.8 | 5.02 | Recover | 2.8 / 148 |
   | tap016 | −0.5 | 5.82 | Reach | 27.8 / 103 |
   | rough | −0.2 | 3.30 | Reach | 16.4 / 125 |
   Muhtemel sebep: `FreeFootRise` hedef acidan hesapliyor, ekrandaki aci `Track` (ve `limbLag`) sonrasi. Taban gosterilen aciyla, her seyi hareket ettiren son adimdan sonra alinmali; yeni uzatilan kosu anahtarlarinda da ayni. Temas eden ayagi bastirma.
2. **On ayak yukarida:** kullanicinin acik istegi. Yerdeyken ayak ucu >= 35 cm olmali; T-118'de `balanceLeg` 62/148'e indi ve kalca yayi yuzunden ~17 cm kalabilir. Kalca en alttayken bile saglayacak aci ya da ayak yuksekligine gore sureklı aci sec.
3. **`HopPlant` esigi surekli olsun:** `_hop * (1 - _pair) > 0.5` kullanicinin "ozel durum yok" kuralina aykiri, tek tus ile sirali arasinda tutukluk yapabilir.
4. **Duzensiz tek tus ve gecis:** `hopRough` −21/+52.6 cm, `swap` −30/+54 cm ayak atlamasi (`foot_pops.py`, T-118 oncesi). 07d3b5c4 bunlari kapatiyor mu koddan dogrula, kalan yolu kapat. Press ayak havadayken de yerdeyken de gelebilir: indigi yere iner, isinlanmaz.
5. **Zincirin kalani:** uyluk -> diz -> ayak perdesi, omuz -> dirsek -> el. Ayak perdesi ve el henuz gecikmesiz; ekle. Kullaniciya soylendi.
6. **Temizlik:** `StartSwing` ve `Enter`'deki cift gosterilen-aci blogunu teke indir.

## Kapsam disi
- `RunnerSim.cs`, prefab, sahne. Unity, batchmode, verify, probe.

## Calisma notu
- 1 (457f6f62): FreeFootRise aslinda gosterilen aciyi (Track sonrasi) okuyordu, hedefi degil; supheyi cürüttum. Sebep: tahmin birkac derece sapar ve Reach/Rest (carry) bacaklari FreeFootRise'a girmez. Cozum: PoseAnkle sonunda gercek kemikte top zemin altindaysa ayak yukari doner (son adim).
- 2 (2c05512a): balanceLeg 80/140, balanceLift 22: kalca yukselince bacak alcalir.
- 3 (85632de2): HopPlant esigi kalkti; inen ayak partner Hold/Reach degilse her ritimde civilenir.
- 4: Enter + catch yollari kodda dogrulandi, ek yol bulunmadi (olculmedi).
- 5 (7980816b): ayak salinim egrileri FootLag 14, FootBlend 18; el icin ayri eklem yok.
- 6 (c56b10a2): StartSwing'deki cift blok kalkti.
- Emin degilim: PoseAnkle clip'i planted ayakta 3 mm'ye kadar ayagi hafif cevirebilir; HopPlant kalkinca gec basisli kosuda (late050) stance davranisi degisir.
