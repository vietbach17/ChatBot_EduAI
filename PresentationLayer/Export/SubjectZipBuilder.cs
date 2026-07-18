using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BussinessLayer.DTOs;

namespace PresentationLayer.Export
{
    /// <summary>
    /// Gom toan bo tai lieu cua mot mon hoc thanh file zip (thu muc theo chuong).
    /// Dung chung cho trang giang vien (ManageSubject) va sinh vien (SubjectDetail).
    /// </summary>
    public static class SubjectZipBuilder
    {
        /// <summary>
        /// Tra ve stream zip chua tat ca tai lieu cua mon, hoac null neu khong co file nao ton tai tren dia.
        /// </summary>
        public static MemoryStream? Build(SubjectDto subject, string contentRootPath)
        {
            var entries = new List<(DocumentDto Doc, string Folder)>();
            foreach (var doc in subject.Documents.Where(d => d.ChapterId == null && !d.IsDeleted))
            {
                entries.Add((doc, "Tai lieu chung"));
            }
            foreach (var chapter in subject.Chapters.OrderBy(c => c.OrderIndex))
            {
                foreach (var doc in chapter.Documents.Where(d => !d.IsDeleted))
                {
                    entries.Add((doc, SanitizeZipName(chapter.Title)));
                }
            }

            // File có thể nằm ở App_Data/files (mới) hoặc wwwroot/files (tài liệu cũ)
            var searchDirs = new[]
            {
                Path.Combine(contentRootPath, "App_Data", "files"),
                Path.Combine(contentRootPath, "wwwroot", "files")
            };

            var zipStream = new MemoryStream();
            var addedCount = 0;
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (doc, folder) in entries)
                {
                    var storedName = Path.GetFileName(doc.FileUrl ?? string.Empty);
                    if (string.IsNullOrEmpty(storedName)) continue;

                    var physicalPath = searchDirs.Select(dir => Path.Combine(dir, storedName)).FirstOrDefault(File.Exists);
                    if (physicalPath == null) continue;

                    var ext = Path.GetExtension(storedName);
                    var baseName = SanitizeZipName(string.IsNullOrWhiteSpace(doc.Title) ? Path.GetFileNameWithoutExtension(storedName) : doc.Title);
                    var entryName = $"{folder}/{baseName}{ext}";
                    for (var i = 2; !usedNames.Add(entryName); i++)
                    {
                        entryName = $"{folder}/{baseName} ({i}){ext}";
                    }

                    archive.CreateEntryFromFile(physicalPath, entryName);
                    addedCount++;
                }
            }

            if (addedCount == 0)
            {
                zipStream.Dispose();
                return null;
            }

            zipStream.Position = 0;
            return zipStream;
        }

        private static string SanitizeZipName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string((name ?? string.Empty).Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return string.IsNullOrEmpty(cleaned) ? "Tai lieu" : cleaned;
        }
    }
}
