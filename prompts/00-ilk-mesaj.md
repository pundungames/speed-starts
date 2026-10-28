# İLK MESAJ — yeni projede ne yazacaksın

Bu makinede ilk kez kullanıyorsan paneli bir kez kur:
`claude plugin install pano --marketplace D:/Pundun/agent-kit -s user`

Sonra yeni projenin Claude oturumuna şunu yapıştır (`<>` yerlerini doldur, durumuna uymayan iki maddeyi sil):

```
Bu projeye D:\Pundun\agent-kit'teki agent kit v2'yi kur ve bundan sonra bu düzenle çalış.

1. Önce D:\Pundun\agent-kit\README.md'yi oku (agent bölümü), sonra 07-AKIS.md'yi.
2. Proje git deposu değilse git init yap ve bir çalışma dalı aç.
3. Kurulum: powershell -File D:\Pundun\agent-kit\install.ps1 -Proje <proje kökü> -Kuru
   Ne yazacağını bana göster; onaylarsam -Kuru olmadan çalıştır.
4. Doldur: agent-kit.config.psd1'deki <KÖŞELİ> yerler, CLAUDE.md'nin projeye özgü bölümleri
   (motor/sürüm, kompozisyon kökü, kod standardı), doğrulama script'i (scripts/verify.ps1.ORNEK sözleşmesiyle).
   Bilemediğin yerleri bana tek listede sor, uydurma.
5. Hook'ları dene: bir .prefab yazmayı ve "git add -A"yı dene, ikisi de engellenmeli. Engellenmezse dur, söyle.
6. docs/DURUM.md'yi yaz; ilk satır: MOD: süpervizör
7. Pano yan paneli açık değilse söyle (kurulum satırı README'de); önemli bulguları bildir ile panoya yaz.
8. Durumum:
   - GDD var: <dosya yolu> → agent-kit/prompts/gdd-bootstrap.md'yi uygula, Gün 0'da kod yazma.
   - GDD yok, fikir şu: <2-3 cümle> → agent-kit/01-BASLANGIC-PROTOKOLU.md Gün 0.
   - Proje zaten ilerlemiş → docs/TEMEL-SOZLESME.md'yi koda bakarak çıkar; eski task dosyaları varsa
     scripts/task-gocu.ps1'i önce KURU çalıştırıp raporu göster; denetci-agent ile ölü ayar taraması yap.
Bitince "⚠ Dikkat / Senden beklenen / Yapılan" biçiminde kısa rapor ver.
```

Aşağıdaki A / B / C, aynı işin daha kısa ayrı ayrı halleridir.

---

## A. Elinde GDD varsa

```
GDD: docs/gdd/<dosya>.md
agent-kit kurulu.

agent-kit/prompts/gdd-bootstrap.md'yi uygula. Gün 0'dan başla:
kod yazma, önce docs/TEMEL-SOZLESME.md'yi çıkar ve mimariyi değiştiren
soruları tek seferde sor. Sayıları belgeden asset'e taşı, koda değil.
```

Beklenen çıktı: `docs/TEMEL-SOZLESME.md` + en fazla 7 soru. Kod yok.

## B. Elinde GDD yoksa, fikir var

```
Yapmak istediğim: <2-3 cümle>
agent-kit kurulu.

agent-kit/01-BASLANGIC-PROTOKOLU.md'yi uygula ama GDD yok —
Gün 0'ı benim anlattığımdan çıkar. Önce dikey dilimi tanımla,
sonra mimariyi değiştiren soruları sor.
```

## C. Devam eden bir projeye kiti sonradan takıyorsan

```
agent-kit kurulu ama proje zaten ilerlemiş durumda.
1. Gün 0'ı GERİYE DÖNÜK yap: mevcut koda ve asset'lere bakarak
   docs/TEMEL-SOZLESME.md'yi çıkar. Belgeye değil KODA bak.
2. Eski task dosyaları varsa scripts/task-gocu.ps1'i önce KURU çalıştır,
   raporu bana göster, onaylarsam -Uygula.
3. docs/DURUM.md'yi bugünkü halden yaz.
4. Bir ölü ayar taraması yap (denetci-agent): her config alanının gerçek bir okuyucusu var mı?
```

Bu üçüncü durum en sık karşılaşılanı ve en değerlisi: mevcut projenin gerçek mimarisini yazıya dökmek,
çoğu zaman üç dört sessiz hatayı ortaya çıkarır.

---

## Her gün — kısa mesajlar

| Ne diyorsun | Ne olur |
|---|---|
| `devam` / `oturum başı` | DURUM.md → ListAgents → git durumu; sonra kaldığı yerden |
| `süpervizör ol` | DURUM.md ilk satırı `MOD: süpervizör`; her kod işi agent'a |
| `hızlı mod` / `sen yap bu tarz işleri` | `MOD: hızlı`; 5 dakikalık işleri kendisi yapar |
| `checkpoint at` | tur-sonu kapısı + `docs/checkpoints/<tarih>.md` + DURUM.md baştan + "kite ders?" |
| `geçir` / `tamam` | doğrulanmış işi ana dala ff; `tamam` → task `done` |
| `bu dalda yap` | worktree yerine ana klasörde (kullanıcı Unity'de çalışmıyorken) |

Durumu değiştirdiğinde tek satır yaz — en çok tur kaybettiren şey sessiz değişiklikti:
`Unity açık, X prefab'ını düzenliyorum` · `pig HP'yi elle değiştirdim, dokunma` · `branch: Y`.
