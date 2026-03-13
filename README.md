# HWID Viewer

Aplikasi Windows sederhana untuk menampilkan identifier perangkat milik Anda sendiri tanpa memodifikasi sistem.

## Fitur

- Menampilkan `Machine GUID`, `System UUID`, serial BIOS, serial motherboard, CPU ID, model disk, serial disk, dan MAC address utama
- Membuat fingerprint lokal berbasis `SHA-256`
- Tombol `Refresh` untuk baca ulang data
- Tombol `Copy All` untuk menyalin seluruh hasil
- Tombol `Save Baseline` untuk menyimpan snapshot acuan
- Tombol `Compare` untuk membandingkan kondisi saat ini vs baseline dan menyorot field yang berubah
- Tombol `History` untuk melihat riwayat compare/simpan baseline

## Struktur

- `HwidViewer/Program.cs` - source WinForms utama
- `build.bat` - build menjadi `dist\HwidViewer.exe`

## Cara Build

1. Pastikan .NET Framework compiler tersedia di Windows.
2. Jalankan `build.bat`.
3. Hasil `.exe` akan dibuat di `dist\HwidViewer.exe`.

## Catatan

- Beberapa field bisa tampil `Unavailable` tergantung hardware, driver, atau izin WMI.
- Fingerprint ini hanya ringkasan lokal dari data yang terbaca, bukan identitas resmi dari Microsoft atau vendor hardware.
- Baseline disimpan di `%APPDATA%\HwidViewer\baseline.txt`
- Riwayat disimpan di `%APPDATA%\HwidViewer\history.log`
