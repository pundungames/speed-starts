MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-11-06. Son checkpoint `docs/checkpoints/2026-11-05-karakter-ik.md`. Push kullanıcıda.

## 06.11: kullanıcının üç notu kapandı, gözünü bekliyor (`IKTest`)
- **T-145 bacak:** uyluk yüksekte diz kıvrık, inerken baldır öne (`tuckPeak` 86/82, `shinThrow` 56/160, `tuckThrowKeys`, `tuckLead` 0.05; `Catch`'te diz önce açılır). Tepe / diz / baldır: ritim 81/84/+30, en hızlı 78/82/+25, yavaş 73/90/+25, boğma 69/92/+17.
- **T-146 kol:** uyluktan gelen sürüş kol başına yaydan geçer, Hz = `armPumpRate` 1 / basış aralığı; `armPumpWidth` 1.12 genişliği geri verir. Kare hareketi ritim 15 → 9.5, en hızlı 21 → 14.5, yavaş 8 → 5.7.
- **T-147 geçiş:** `_hopBody` (`hopBodySpring` 2 Hz) yalnız girişte yavaş; kalça 8.3 → 3.9 derece/kare, ilk sekiş 26 → 12 cm; Rest'e giren bacağın iki karelik carry'si kalktı. Çıkış (`hopToRun`) ve kol atışı değişmedi.
- Önce ölçüsü `t144-after`; son kayıt `t147-a`. Kullanıcı bakınca yorumuna göre devam.

## Çalışma düzeni
- Kullanıcı `IKTest`'te (ana klasör, Unity açık). İş `agent/ik-kosucu`'da, worktree `D:\Pundun\speed-starts-ik`. Geçirme: kopyada doğrula, ana klasörde `git merge --ff-only <hash>`. `agent/ik-govde` worktree'si silinebilir.
- Kullanıcı: "yapıldıkça bana geçir", süre vaadi verme, input yalnız hedef değiştirir, snap yok. Titreme ve geçiş işinde seri kare al ("kare kare çok aralıklı alıyorsun"). Kör turda tek değişiklik, ayrı commit.
- Malik'in dalı (`claude/karakter-hareketleri-baec15`) **birleştirilmez** (kullanıcı). Oradan elle alınanlar: tutma ve dalış (T-129), sırt ve kafa (T-144, `UstGovdeAktarim.md` bölüm 2, 4). Malik'in kolları alınmadı. Onun `PhysicalTest.unity` commit'i engelleri siliyor.

## Karakterde oturanlar (05.11, T-128..T-144 `review`)
- Yerdeki ayak öne gideni bekler (`OtherTakesOver`), çivili ayak basışta yerinde kalır, tutma ancak 0.22 sn sonra (`holdAfter`), `Track` yalnız hızlanmayı sınırlar (fren ×4), ağırlıklar yayla, basma karesinde `plantGap`.
- Üst gövde: Malik'in zinciri; `RunnerSim.ShownHeadAngle` yalnız görsel (hızı belirleyen `HeadAngle` aynı). Düz koşuda `runRock`, burgu, eğim kapalı; tek ayak ve çift zıplama gövdesi bizde.
- Koşu zıplaması `runBounce` 0.5, basma pozu `contactRun` 30/145, kamera Y sabit (`trackPull`, `trackSpeedSmooth`), fren `stopBrakeForce` 12.
- Açık: tuş 0.18 sn basılı kalınca gövde kambur (kalça 66°); tek ayakta gövde ~30° geride, denge ayağı alçak; dalışta kollar yukarıda, önce çift zıplıyor; koşudan çift zıplamaya ~0.25 sn gecikme; tuşsuz süzülmede ayak kayar. Hareket oturunca `PlayerController.md`.
- Prefab'ta kayıtlı, kod varsayılanı işlemez: `pushEnd`, `recover`, `mid`, `reach`, `holdBack`, `holdSupport`, `hipShare` 0.65, `liftKick` 1.5, `armBack`; sim `stopDelay`, `stopRamp`. Prefab Editor'den kaydedilirse tüm yeni alanlar dosyaya yazılır ve koddan değişmez: yeni ad aç.
- Tam takım 06.11 (`51d680fa`): EditMode 1281/1285, PlayMode 98/103 (3 atlanan); 6 kırmızı, hepsi backlog'da, koşucu dışı.

## Ölçüm
- Kayıt: `powershell -File scripts/runner-probe.ps1 -Kaynak D:\Pundun\speed-starts-ik -Etiket x -Sahip T-xxx -Filtre RunnerTransitions_WriteFrameStrips` (yaklaşık 1 dk, commit'li hâli ölçer). Çıktı `D:\Pundun\form-probe\x\`: `trans_<senaryo>.csv` her kare, `trans_<senaryo>_strip<i>.png` 40 ardışık kare.
- Analiz: `python -I scripts/runner-olcum/transition_check.py <önce> [<sonra>]`. Geri oturmada `thigh` sütunu ekrandaki değil: ayak ve kalça konumuyla doğrula.
- Sim: `scripts/runner-olcum/simprobe.csproj`, `dotnet build -c Release`, exe'yi doğrudan çalıştır.

## Verilen kararlar
- Kullanıcı 03.11: "referans oyun ne ise aynısı". Ekonomi ve fiyatlar `docs/decisions/ADR-2026-11-03-ekonomi-v2-referans.md`. Gerçek gelir, reklam SDK, IAP şimdilik yok (Çin müşterisi). TMP + American Captain her yerde. Builder var olanı ezmez (`-forceOverwriteHandEdits` kullanma).
- Tema fiyatları: Egypt 2.500, Rome 5.000, Pirate Sea 7.500 Nakit, Sky Islands 50 Mor Mücevher.
- Installer bağlamaları: kalıcı onay. Teslim belgesi, telefonda APK, ana menü hızı: kullanıcı "geç" dedi. Günlük build Mono/ARMv7, mağaza IL2CPP.
- Kullanıcı görsün (sim, dokunulmadı): ezmede 100 m 22+ sn; tuş süresi ritim 0.28'de 0.13 sn = 11.1 sn, 0.17 sn = 13.1 sn, 0.20 sn = 29.5 sn.

## Kullanıcıdan / başkalarından bekleyen
- Malik: UI üstündeki dokunma; engel teması; 4×100m ve Medley; dev bitiş tuşu (F) replay'i bozuyor (T-094).
- Serhat: `BridgeCanalRiverside.prefab`; `PastelBuildingRow` sahne açılınca bina üretiyor; `ThemeRunPlayground.SnapCamera` artık yüksek başlıyor (T-138). Kırmızılar `_backlog.md`'de.
- Uygulama/paket adı, keystore, Privacy URL: sonra. Telefonda henüz denenmedi.

## Dikkat
- PowerShell'de `git` bozuk: git Bash'ten. Kancada `git commit` + "main" aynı komutta olmaz. `ProjectSettings/EditorSettings.asset` kullanıcıda commit'siz, dokunma.

<!-- OTOMATIK:BASLA -->
_tur-sonu.ps1 2026-11-06 11:18 - otomatik blok, elle yazilan bu blokta kaybolur._

- **KAPI TEMIZ (cikis 0)**
- Acik needs: 3 (DECISION 3)
- Review kuyrugu: 28  UYARI: 10 ustu, supervizor kuyrugu birikti
- Goz bekleyen (merged): 110 - T-147, T-146, T-145, T-114, T-115, T-113, T-111, T-109
- in-progress: 0 / blocked: 0
- Uyari: govde-needs 0, test 10, analytics 0 (kapsam: status in-progress/review/merged)
- GIT: 4 calisma agaci (ana: IKTest); ana agacta commit'siz .prefab/.unity/.asset: 1
- KOPYALAR: 2 (kopya.ps1 -Liste -Json)
<!-- OTOMATIK:BITIR -->
