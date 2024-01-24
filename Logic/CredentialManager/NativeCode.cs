﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ProjectDoxen.Logic.CredentialManager
{
    internal static class NativeCode
    {
        [Flags]
        internal enum CredentialUIFlags
        {
            IncorrectPassword = 0x1,
            DoNotPersist = 0x2,
            RequestAdministrator = 0x4,
            ExcludeCertificates = 0x8,
            RequireCertificate = 0x10,
            ShowSaveCheckBox = 0x40,
            AlwaysShowUi = 0x80,
            RequireSmartcard = 0x100,
            PasswordOnlyOk = 0x200,
            ValidateUsername = 0x400,
            CompleteUsername = 0x800,
            Persist = 0x1000,
            ServerCredential = 0x4000,
            ExpectConfirmation = 0x20000,
            GenericCredentials = 0x40000,
            UsernameTargetCredentials = 0x80000,
            KeepUsername = 0x100000
        }

        internal enum CredentialUIReturnCodes : uint
        {
            Success = 0,
            Cancelled = 1223,
            NoSuchLogonSession = 1312,
            NotFound = 1168,
            InvalidAccountName = 1315,
            InsufficientBuffer = 122,
            InvalidParameter = 87,
            InvalidFlags = 1004
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CredentialUIInfo
        {
            public int cbSize;
            public nint hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public nint hbmBanner;
        }

        [DllImport("credui", CharSet = CharSet.Unicode)]
        internal static extern CredentialUIReturnCodes CredUIPromptForCredentials(ref CredentialUIInfo creditUR,
          string targetName,
          nint reserved1,
          int iError,
          StringBuilder userName,
          int maxUserName,
          StringBuilder password,
          int maxPassword,
          [MarshalAs(UnmanagedType.Bool)] ref bool pfSave,
          CredentialUIFlags flags);

        [DllImport("credui.dll", EntryPoint = "CredUIParseUserNameW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern CredentialUIReturnCodes CredUIParseUserName(
                string userName,
                StringBuilder user,
                int userMaxChars,
                StringBuilder domain,
                int domainMaxChars);

        [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredPackAuthenticationBuffer(
            int dwFlags,
            StringBuilder pszUserName,
            StringBuilder pszPassword,
            nint pPackedCredentials,
            ref int pcbPackedCredentials
        );

        [DllImport("credui.dll", CharSet = CharSet.Unicode)]
        internal static extern bool CredUnPackAuthenticationBuffer(int dwFlags,
            nint pAuthBuffer,
            uint cbAuthBuffer,
            StringBuilder pszUserName,
            ref int pcchMaxUserName,
            StringBuilder pszDomainName,
            ref int pcchMaxDomainame,
            StringBuilder pszPassword,
            ref int pcchMaxPassword);

        [DllImport("credui.dll", EntryPoint = "CredUIPromptForWindowsCredentialsW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int CredUIPromptForWindowsCredentials(ref CredentialUIInfo creditUR,
            int authError,
            ref uint authPackage,
            nint inAuthBuffer,
            int inAuthBufferSize,
            out nint refOutAuthBuffer,
            out uint refOutAuthBufferSize,
            ref bool fSave,
            PromptForWindowsCredentialsFlags flags);

        [DllImport("credui", CharSet = CharSet.Unicode)]
        internal static extern CredentialUIReturnCodes CredUICmdLinePromptForCredentials(
            string targetName,
            nint reserved1,
            int iError,
            StringBuilder userName,
            int maxUserName,
            StringBuilder password,
            int maxPassword,
            [MarshalAs(UnmanagedType.Bool)] ref bool pfSave,
            CredentialUIFlags flags);


        [Flags]
        internal enum PromptForWindowsCredentialsFlags : uint
        {
            GenericCredentials = 0x1,
            ShowCheckbox = 0x2,
            AuthpackageOnly = 0x10,
            InCredOnly = 0x20,
            EnumerateAdmins = 0x100,
            EnumerateCurrentUser = 0x200,
            SecurePrompt = 0x1000,
            Pack32Wow = 0x10000000
        }




        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct NativeCredential
        {
            public uint Flags;
            public uint Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public nint CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public nint Attributes;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string UserName;

        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct NativeCredentialAttribute
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Keyword;
            public uint Flags;
            public uint ValueSize;
            public nint Value;
        }

        [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredDelete([MarshalAs(UnmanagedType.LPWStr)] string target, uint type, int reservedFlag);

        [DllImport("Advapi32.dll", EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredEnumerate([MarshalAs(UnmanagedType.LPWStr)] string target, uint flags, out uint count, out nint credentialsPtr);

        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredRead([MarshalAs(UnmanagedType.LPWStr)] string target, uint type, int reservedFlag, out nint CredentialPtr);

        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredWrite([In] ref NativeCredential userCredential, [In] uint flags);

        [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        internal static extern bool CredFree([In] nint cred);

        [DllImport("ole32.dll", EntryPoint = "CoTaskMemFree", SetLastError = true)]
        internal static extern void CoTaskMemFree(nint buffer);
    }
}
