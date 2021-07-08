﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

#nullable disable

namespace Microsoft.DotNet.NativeWrapper
{
    public static partial class Interop
    {
        static readonly string HostFxrPath;

        static Interop()
        {
            HostFxrPath = (string)AppContext.GetData("HOSTFXR_PATH");
            NativeLibrary.SetDllImportResolver(typeof(Interop).Assembly, HostFxrDllImportResolver);
        }

        static IntPtr HostFxrDllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == "hostfxr")
            {
                NativeLibrary.TryLoad(HostFxrPath, out IntPtr handle);
                return handle;
            }

            return IntPtr.Zero;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr LoadLibraryExW(string lpFileName, IntPtr hFile, int dwFlags);

        [Flags]
        internal enum hostfxr_resolve_sdk2_flags_t : int
        {
            disallow_prerelease = 0x1,
        }

        internal enum hostfxr_resolve_sdk2_result_key_t : int
        {
            resolved_sdk_dir = 0,
            global_json_path = 1,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct hostfxr_dotnet_environment_info
        {
            public nuint size;
            public string hostfxr_version;
            public string hostfxr_commit_hash;
            public nuint sdk_count;
            public IntPtr sdks;
            public nuint framework_count;
            public IntPtr frameworks;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct hostfxr_dotnet_environment_framework_info
        {
            public nuint size;
            public string name;
            public string version;
            public string path;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct hostfxr_dotnet_environment_sdk_info
        {
            public nuint size;
            public string version;
            public string path;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        internal delegate void hostfxr_get_dotnet_environment_info_result_fn(
            IntPtr info,
            IntPtr result_context);

        [DllImport("hostfxr", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int hostfxr_get_dotnet_environment_info(
            string dotnet_root,
            IntPtr reserved,
            hostfxr_get_dotnet_environment_info_result_fn result,
            IntPtr result_context);

        public static class Windows
        {
            private const CharSet UTF16 = CharSet.Unicode;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = UTF16)]
            internal delegate void hostfxr_resolve_sdk2_result_fn(
                hostfxr_resolve_sdk2_result_key_t key,
                string value);

            [DllImport("hostfxr", CharSet = UTF16, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hostfxr_resolve_sdk2(
                string exe_dir,
                string working_dir,
                hostfxr_resolve_sdk2_flags_t flags,
                hostfxr_resolve_sdk2_result_fn result);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = UTF16)]
            internal delegate void hostfxr_get_available_sdks_result_fn(
                int sdk_count,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
                string[] sdk_dirs);

            [DllImport("hostfxr", CharSet = UTF16, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hostfxr_get_available_sdks(
                string exe_dir,
                hostfxr_get_available_sdks_result_fn result);
        }

        public static class Unix
        {
            // Ansi marshaling on Unix is actually UTF8
            private const CharSet UTF8 = CharSet.Ansi;
            private static string PtrToStringUTF8(IntPtr ptr) => Marshal.PtrToStringAnsi(ptr);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = UTF8)]
            internal delegate void hostfxr_resolve_sdk2_result_fn(
                hostfxr_resolve_sdk2_result_key_t key,
                string value);

            [DllImport("hostfxr", CharSet = UTF8, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hostfxr_resolve_sdk2(
                string exe_dir,
                string working_dir,
                hostfxr_resolve_sdk2_flags_t flags,
                hostfxr_resolve_sdk2_result_fn result);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = UTF8)]
            internal delegate void hostfxr_get_available_sdks_result_fn(
                int sdk_count,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
                string[] sdk_dirs);

            [DllImport("hostfxr", CharSet = UTF8, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hostfxr_get_available_sdks(
                string exe_dir,
                hostfxr_get_available_sdks_result_fn result);

            [DllImport("libc", CharSet = UTF8, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr realpath(string path, IntPtr buffer);

            [DllImport("libc", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            private static extern void free(IntPtr ptr);

            public static string realpath(string path)
            {
                var ptr = realpath(path, IntPtr.Zero);
                var result = PtrToStringUTF8(ptr);
                free(ptr);
                return result;
            }
        }
    }
}
