MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-11-06. Son checkpoint `docs/checkpoints/2026-11-05-karakter-ik.md`. Push kullanıcıda.

## 06.11: tek ayak sekiş turu, gözünü bekliyor (`main`, T-145..T-161 `merged`)
- **T-157 (kullanıcı denedi, kural):** her hareket basıştan, basış yoksa direkt stop. Niyet ham tuştan (`NoteIntent`, `_hopWanted`), ağırlık alınan basıştan. Tek basış tamponu (`hopBufferTime`). Sekiş "kendi döngüsü" iptal.
- **T-154 sekiş:** sert kapı (`hopTime`, ayak havadayken basış alınmaz, kurulu olmayan ilk sekişte diğer tuş geçer), yay 0.3 m, geri atış -57, serbest bacak `hopFreeLeg` sabit, kol biri sabit biri bacağa bağlı, `hopLean`. Dururken tek basış sekiş (`hopStandWait`). `hipMaxAccel`.
- **T-155 tek tuş hız:** seride taban (`hopFloorSpeed` 4.2, kullanıcı 5.6 için "çok fazla" dedi), geç sayılmaz, baş bozmaz; fren ritme göre bekler (`stopWaitShare`). İki tuş sayıları birebir aynı. **T-156 kalça:** kök üstünde ileri-geri (`hopSurge`, `runSurge`), koşuda burgu.
- **T-158:** dururken tek basış tam sekiş, spam %50 kapısı, bacak değiştirmede basış ayak değince çalışır ve eski bacak adım gibi çıkar, basışta gövde geri (`pressBackKick`, sekişte hâlâ ~1 derece), hızlıda `tuckPeakFast`. Önceki T-145..T-153.
- **T-159 kamera:** `sideYaw` 4, `speedZoomFull` 11, `speedZoomDistance` 2.4 (`Race.unity` eski adları tutar, okunmaz). Son kayıt `t161-a`. `RunnerDtJitter` koşulmadı. **T-160 bayrak ekibi:** `IRelayTeam`, Home `RelayLeg`, `MenuRelayTeam`, yarışta `UseTeam`; Malik oyuncuyu tüm takıma koştu. **T-161:** `Animations/Start/RunnerStartPoses.fbx`, `RunnerStartPose`. Araçlar: `swing_table.py`, `arm_table.py`, CSV `d_posePresses`, `L_handY`. Sayı değişince Editor de prefab Reimport (TUZAK).

## Çalışma düzeni
- Kullanıcı 06.11 `main`'e geçti (ana klasör, Unity açık; `IKTest` kapandı). İş `agent/ik-kosucu`'da, worktree `D:\Pundun\speed-starts-ik`. Geçirme: kopyada doğrula, ana klasörde `git merge --ff-only <hash>`. `agent/ik-govde` worktree'si silinebilir.
- Kullanıcı: "yapıldıkça bana geçir", süre vaadi verme, input yalnız hedef değiştirir, snap yok. Titreme ve geçiş işinde seri kare al ("kare kare çok aralıklı alıyorsun"). Kör turda tek değişiklik, ayrı commit.
- Malik'in dalı (`claude/karakter-hareketleri-baec15`) **birleştirilmez** (kullanıcı). Oradan elle alınanlar: tutma ve dalış (T-129), sırt ve kafa (T-144, `UstGovdeAktarim.md` bölüm 2, 4). Malik'in kolları alınmadı. Onun `PhysicalTest.unity` commit'i engelleri siliyor.

## Karakterde oturanlar (05.11, T-128..T-144 `review`)
- Yerdeki ayak öne gideni bekler (`OtherTakesOver`), çivili ayak basışta yerinde kalır, tutma ancak 0.22 sn sonra (`holdAfter`), `Track` yalnız hızlanmayı sınırlar (fren ×4), ağırlıklar yayla, basma karesinde `plantGap`.
- Üst gövde: Malik'in zinciri; `RunnerSim.ShownHeadAngle` yalnız görsel (hızı belirleyen `HeadAngle` aynı). Düz koşuda `runRock`, eğim kapalı, burgu T-156 ile açık; tek ayak ve çift zıplama gövdesi bizde.
- Koşu zıplaması `runBounce` 0.5, basma pozu `contactRun` 30/145, kamera Y sabit (`trackPull`, `trackSpeedSmooth`), fren `stopBrakeForce` 12.
- Açık: tuş 0.18 sn basılı kalınca gövde kambur (kalça 66°); dalışta kollar yukarıda, önce çift zıplıyor; koşudan çift zıplamaya ~0.25 sn gecikme; tuşsuz süzülmede ayak kayar. Hareket oturunca `PlayerController.md`.
- Prefab'ta kayıtlı, kod varsayılanı işlemez: `pushEnd`, `recover`, `mid`, `reach`, `holdBack`, `holdSupport`, `hipShare` 0.65, `liftKick` 1.5, `armBack`; sim `stopDelay`, `stopRamp`. Prefab Editor'den kaydedilirse tüm yeni alanlar dosyaya yazılır ve koddan değişmez: yeni ad aç.
- Tam takım 06.11 (`13d3ef87`, birleşik `main`): EditMode 4 kırmızı, PlayMode 101/110 (7 atlanan: 3 ölçüm, 4 yarış testi bilet yüzünden); 6 kırmızı hep aynı, koşucu dışı.

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
_tur-sonu.ps1 2026-11-06 13:28 - otomatik blok, elle yazilan bu blokta kaybolur._

- **KAPI TEMIZ (cikis 0)**
- Acik needs: 3 (DECISION 3)
- Review kuyrugu: 28  UYARI: 10 ustu, supervizor kuyrugu birikti
- Goz bekleyen (merged): 116 - T-153, T-152, T-151, T-149, T-150, T-148, T-147, T-146
- in-progress: 0 / blocked: 0
- Uyari: govde-needs 0, test 10, analytics 0 (kapsam: status in-progress/review/merged)
- GIT: 4 calisma agaci (ana: IKTest); ana agacta commit'siz .prefab/.unity/.asset: 1
- KOPYALAR: 2 (kopya.ps1 -Liste -Json)
<!-- OTOMATIK:BITIR -->
