# Analisis Pengujian Arsitektur Data-Driven Architecture
## Structural Testing Analysis Report

---

## PENDAHULUAN

Pengujian arsitektur data-driven pada project Syncara-Sky dilakukan berdasarkan 10 skenario yang terdapat dalam dokumen ArsitekturalTesting.docx. Analisis ini bertujuan untuk mengevaluasi efektivitas penggunaan ScriptableObject sebagai sumber data utama untuk weapon, payload, dan vehicle system. Fokus utama pengujian adalah pemisahan data dan logika, kemudahan penambahan konten, skalabilitas sistem, dukungan terhadap kolaborasi designer, maintainability, reusability, error handling, serta integrasi data ke gameplay.

Hasil pengujian menunjukkan bahwa sistem utama pada repo ini lebih dominan menggunakan pendekatan data-driven dibanding hard-coded/native factory. Data senjata berada pada `Guns`, data payload berada pada `Payload`, dan data kendaraan berada pada `Vehicles`. Runtime component seperti `Gun`, `PayloadManager`, `GunSelector`, `PayloadSelector`, dan `GameSelectionManager` menggunakan data tersebut untuk mengatur behavior dan loadout pemain.

---

## ANALISIS SKENARIO D1 - PENAMBAHAN SENJATA

Skenario D1 menguji kemampuan sistem untuk menambahkan senjata baru ke dalam game. Pada arsitektur data-driven yang digunakan repo ini, penambahan senjata baru dilakukan dengan membuat asset `Guns` ScriptableObject baru. Asset tersebut menyimpan nama senjata, deskripsi, artwork, prefab peluru, sound key, damage, fire rate, bullet speed, heat rate, tier, dan price. Setelah asset dibuat, data dapat dimasukkan ke `availableGuns` pada `GunSelector`.

Hasil pengujian menunjukkan bahwa penambahan senjata baru tidak membutuhkan perubahan pada logic utama `Gun.cs` selama senjata tersebut masih mengikuti behavior firing yang sama. Ini merupakan keunggulan utama data-driven architecture karena konten baru dapat ditambahkan melalui data, bukan melalui penambahan class atau method factory baru. Namun, sistem belum sepenuhnya otomatis karena item UI masih perlu disiapkan atau disesuaikan secara manual. Dengan demikian, penambahan senjata sudah lebih cepat dibanding hard-coded approach, tetapi masih memiliki ketergantungan pada setup Inspector dan UI.

**Status:** Partial Pass

---

## ANALISIS SKENARIO D2 - PERUBAHAN PARAMETER

Skenario D2 mengevaluasi perubahan parameter seperti damage dan fire rate. Pada repo ini, parameter utama senjata disimpan di `Guns` ScriptableObject dan dapat diedit langsung melalui Unity Inspector. Selain itu, validasi data sudah disediakan melalui `OnValidate()` pada `Guns.cs`, sehingga nilai seperti damage, bullet speed, fire rate, heat rate, tier, dan price dapat dikoreksi agar tetap berada dalam range yang aman.

Runtime validation juga tersedia di `Gun.ValidateGunData()`. Hal ini memperkuat keamanan sistem karena data yang diterapkan ke gameplay akan kembali diperiksa sebelum digunakan. Perubahan parameter tidak memerlukan perubahan kode logic dan tidak perlu membuat method factory baru. Designer atau developer dapat melakukan tuning dengan cepat melalui data asset.

**Status:** Pass

---

## ANALISIS SKENARIO D3 - VARIASI LOADOUT

Skenario D3 menguji kemampuan sistem dalam menggunakan berbagai kombinasi loadout. Sistem mendukung variasi loadout melalui `GunSelector`, `PayloadSelector`, dan `GameSelectionManager`. Gun dan payload yang dipilih disimpan sebagai data terkonfirmasi, lalu diterapkan kembali pada gameplay melalui komponen runtime.

Sistem juga menerapkan filter tier berdasarkan kendaraan yang dipilih. Artinya, kendaraan dengan tier rendah tidak dapat mengakses gun atau payload yang memiliki tier lebih tinggi. Ini menunjukkan bahwa variasi loadout tidak hanya bergantung pada daftar statis, tetapi juga pada aturan data yang dapat dikonfigurasi. Payload yang sama juga dapat digabung di `PayloadManager`, sehingga kombinasi slot dapat diproses lebih efisien.

**Status:** Pass

---

## ANALISIS SKENARIO D4 - SKALABILITAS SISTEM

Skenario D4 mengevaluasi skalabilitas sistem ketika jumlah variasi senjata bertambah. Dari sisi data, sistem cukup scalable karena variasi baru dapat ditambahkan sebagai asset ScriptableObject baru. Runtime logic tidak perlu bertambah setiap kali ada senjata baru, selama senjata tersebut masih menggunakan pola behavior yang sama.

Namun, skalabilitas praktis masih dibatasi oleh proses registrasi manual. `GunSelector` masih menggunakan array `availableGuns`, sedangkan UI item juga masih bergantung pada komponen yang ditempatkan secara manual di scene. Jika jumlah senjata meningkat secara signifikan, maintenance UI dan array Inspector dapat menjadi berat. Dengan demikian, data layer sudah scalable, tetapi content registry dan UI generation belum sepenuhnya scalable.

**Status:** Partial Pass

---

## ANALISIS SKENARIO D5 - KOLABORASI DESIGNER

Skenario D5 berfokus pada keterlibatan game designer dalam mengubah parameter. Data-driven architecture pada repo ini sangat mendukung kolaborasi designer karena parameter weapon dan payload dapat diedit langsung melalui Inspector tanpa harus mengubah kode. Designer dapat mengatur damage, fire rate, bullet speed, reload time, max ammo, tier, price, serta parameter missile seperti lock radius dan homing angle.

Validasi melalui `OnValidate()` membantu mencegah input yang terlalu berbahaya, seperti nilai negatif atau nilai nol pada parameter yang harus positif. Ini membuat workflow designer lebih aman dan lebih cepat. Designer tetap harus memahami konsekuensi gameplay dari setiap angka, tetapi secara teknis mereka tidak perlu bergantung pada programmer untuk perubahan balancing dasar.

**Status:** Pass

---

## ANALISIS SKENARIO D6 - MAINTAINABILITY

Skenario D6 mengevaluasi kemudahan maintenance sistem. Pemisahan data ke ScriptableObject membuat maintainability meningkat karena perubahan data tidak bercampur langsung dengan perubahan logic. `Guns`, `Payload`, dan `Vehicles` menjadi tempat utama untuk mendefinisikan konten, sementara runtime component bertugas membaca dan menerapkan data tersebut.

Meskipun demikian, beberapa bagian logic masih cukup padat, terutama `Gun.cs`. File tersebut menangani input firing, heat system, projectile limit, stage firing, spawn bullet, validasi runtime, sound playback, dan VFX playback. Hal ini membuat maintainability masih belum ideal karena banyak tanggung jawab berada dalam satu class. Sistem akan lebih mudah dirawat jika firing, projectile spawning, validation, dan effect playback dipisahkan ke service atau helper yang lebih kecil.

**Status:** Partial Pass

---

## ANALISIS SKENARIO D7 - REUSABILITY

Skenario D7 menguji kemampuan sistem untuk menggunakan ulang data dan logic. Hasil analisis menunjukkan bahwa reusability cukup baik. Satu `Gun` component dapat memakai banyak asset `Guns` berbeda melalui `ApplyGunProperties()`. Payload juga dapat digunakan ulang di berbagai slot dan aircraft selama prefab serta hardpoint sesuai.

`PayloadManager` bahkan menggabungkan payload yang identik berdasarkan referensi ScriptableObject yang sama. Ini berarti sistem tidak memperlakukan setiap slot sebagai tipe weapon yang benar-benar terpisah jika datanya sama. Mekanisme ini mendukung reuse data dan mengurangi duplikasi konfigurasi.

**Status:** Pass

---

## ANALISIS SKENARIO D8 - ERROR HANDLING

Skenario D8 menguji ketahanan sistem terhadap input data tidak valid. Repo ini sudah memiliki beberapa mekanisme error handling. Pada `Guns.cs` dan `Payload.cs`, `OnValidate()` digunakan untuk membatasi nilai negatif atau tidak logis. Pada runtime, `Gun` juga melakukan validasi ulang terhadap data gun, mengecek `bulletPrefab` sebelum instantiate, membatasi jumlah projectile aktif, membatasi jumlah gun aktif per stage, dan melewati spawn point yang null atau tidak aktif.

Pada sisi payload, `PayloadManager` mengecek payload prefab sebelum instantiate, melewati hardpoint null, mendeteksi slot kosong, dan membatasi reload time agar tetap valid. Namun, error handling belum sepenuhnya sempurna. Beberapa singleton call perlu tetap konsisten null-safe, dan projectile counter dapat berpotensi menjadi negatif jika objek peluru dihancurkan tanpa pernah dihitung sebagai active projectile. Karena itu, error handling sudah meningkat tetapi masih belum sempurna.

**Status:** Partial Pass

---

## ANALISIS SKENARIO D9 - ITERASI BALANCING

Skenario D9 mengevaluasi kecepatan iterasi balancing gameplay. Data-driven architecture memberikan keuntungan besar pada area ini. Parameter utama senjata dan payload dapat diubah langsung di Inspector tanpa perlu membuat class baru, mengubah method factory, atau melakukan perubahan logic. Hal ini membuat proses balancing lebih cepat dan lebih ramah untuk designer.

Dengan adanya `OnValidate()`, perubahan nilai juga lebih aman karena sistem langsung mengoreksi nilai yang berada di luar batas. Perubahan balancing seperti menaikkan damage, menurunkan fire rate, mengatur reload time, atau mengubah harga dapat dilakukan langsung pada data asset. Jika dibandingkan hard-coded factory, pendekatan ini jauh lebih efisien untuk iterasi berulang.

**Status:** Pass

---

## ANALISIS SKENARIO D10 - INTEGRASI SISTEM

Skenario D10 menguji integrasi data-driven system dengan gameplay. Integrasi berjalan melalui beberapa tahap. Pertama, player memilih vehicle, gun, dan payload pada menu. Kedua, pilihan disimpan pada `GameSelectionManager` atau referensi selection terkait. Ketiga, saat gameplay dimulai, vehicle prefab dipanggil, gun data diterapkan ke `Gun`, dan payload data diterapkan ke `PayloadManager`.

Sistem ini menunjukkan integrasi yang cukup kuat antara data, UI selection, persistence, dan runtime gameplay. `PayloadSelector` dapat menyesuaikan jumlah slot berdasarkan aircraft yang dipilih, sedangkan `PayloadManager` dapat memproses ulang slot saat loadout berubah. Kekurangan utama masih berada pada direct `Instantiate()` dan belum adanya layer factory/pooling yang benar-benar terpusat. Namun secara arsitektural, data-driven flow sudah terhubung ke gameplay dengan baik.

**Status:** Pass

---

## ANALISIS KOMPARATIF: KEUNGGULAN DAN KELEMAHAN

Berdasarkan 10 skenario pengujian, data-driven architecture pada repo ini memiliki beberapa keunggulan utama:

1. **Penambahan konten lebih cepat**: Senjata dan payload baru dapat dibuat sebagai asset data.
2. **Balancing lebih efisien**: Parameter dapat diubah lewat Inspector.
3. **Kolaborasi designer lebih baik**: Designer tidak harus mengubah kode.
4. **Reusability tinggi**: Data asset dapat digunakan ulang pada banyak loadout.
5. **Pemisahan data dan logic**: Stat weapon/payload tidak hard-coded langsung di runtime behavior.
6. **Validasi data tersedia**: `OnValidate()` dan runtime validation membantu mengurangi nilai tidak valid.
7. **Integrasi gameplay cukup kuat**: Data dari menu dapat diterapkan ke runtime gameplay.

Kelemahan yang masih ditemukan:

1. **UI registration masih manual**: `availableGuns`, `availablePayloads`, dan item UI masih perlu diatur secara manual.
2. **Belum ada centralized registry**: Data asset belum otomatis ditemukan atau didaftarkan.
3. **`Gun.cs` masih terlalu banyak tanggung jawab**: Input, heat, firing, spawn, sound, VFX, dan validation masih berada dalam satu class.
4. **Direct instantiation masih dominan**: Projectile belum menggunakan pooling.
5. **Error handling belum seragam**: Beberapa edge case masih perlu diperkuat.

---

## KESIMPULAN ANALISIS PENGUJIAN

Pengujian arsitektur data-driven pada Syncara-Sky menunjukkan bahwa sistem sudah berhasil memisahkan data utama dari logic gameplay. Penggunaan ScriptableObject untuk gun, payload, dan vehicle membuat sistem lebih fleksibel dibanding hard-coded factory pattern. Penambahan konten baru, perubahan parameter, variasi loadout, dan iterasi balancing dapat dilakukan lebih cepat karena sebagian besar perubahan terjadi pada data asset.

Secara keseluruhan, dari 10 skenario pengujian, sistem memperoleh 6 status Pass dan 4 status Partial Pass. Tidak ditemukan skenario yang sepenuhnya gagal pada level arsitektur data-driven. Partial Pass muncul terutama karena sistem masih memiliki bagian manual dan beberapa logic runtime yang masih terlalu terkonsentrasi dalam satu class.

Rekomendasi utama untuk pengembangan berikutnya adalah memperkuat tooling data-driven melalui centralized registry, auto-generated UI item, object pooling untuk projectile, dan pemisahan tanggung jawab di `Gun.cs`. Dengan perbaikan tersebut, arsitektur akan menjadi lebih scalable, maintainable, dan siap untuk jumlah konten yang lebih besar.

---

## REKOMENDASI IMPROVEMENTS

1. **Implement Data Registry**  
   Buat registry untuk memuat semua asset `Guns`, `Payload`, dan `Vehicles`, sehingga selector tidak lagi bergantung pada array manual.

2. **Auto Generate UI Items**  
   Gunakan data asset untuk membuat item UI secara otomatis agar penambahan konten tidak membutuhkan setup manual di scene.

3. **Separate Gun Responsibilities**  
   Pisahkan logic firing, validation, projectile spawning, heat, sound, dan VFX dari `Gun.cs` menjadi beberapa komponen/service kecil.

4. **Projectile Pooling**  
   Ganti direct `Instantiate()` pada bullet dan payload projectile dengan object pooling untuk meningkatkan performa.

5. **Centralized Validation Utility**  
   Buat helper validasi bersama agar aturan validasi tidak tersebar di ScriptableObject dan runtime class.

6. **Automated Structural Tests**  
   Tambahkan Unity EditMode tests untuk validasi data, tier filtering, loadout persistence, dan error handling.

---

**Analisis Pengujian Selesai**  
**Date**: 2026-06-13  
**Status**: Complete  
**Target Project**: Syncara-Sky  
**Architecture Under Test**: Data-Driven ScriptableObject Architecture
