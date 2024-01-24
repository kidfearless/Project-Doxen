﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ProjectDoxen.Logic.CredentialManager
{
    internal class Credential : ICredential
    {

        public CredentialType Type { get; set; }
        public string TargetName { get; set; }
        public string Comment { get; set; }
        public DateTime LastWritten { get; set; }
        public string CredentialBlob { get; set; }
        public Persistance Persistance { get; set; }
        public IDictionary<string, object> Attributes { get; set; }
        public string UserName { get; set; }


        public uint Flags;
        public string TargetAlias;

        /// <summary>
        /// Maximum size in bytes of a credential that can be stored. While the API 
        /// documentation lists 512 as the max size, the current Windows SDK sets  
        /// it to 5*512 via CRED_MAX_CREDENTIAL_BLOB_SIZE in wincred.h. This has 
        /// been verified to work on Windows Server 2016 and later. 
        /// <para>
        /// API Doc: https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentiala
        /// </para>
        /// </summary>
        /// <remarks>
        /// This only controls the guard in the library. The actual underlying OS
        /// controls the actual limit. Operating Systems older than Windows Server
        /// 2016 may only support 512 bytes.
        /// <para>
        /// Tokens often are 1040 bytes or more.
        /// </para>
        /// </remarks>
        internal const int MaxCredentialBlobSize = 2560;

        internal Credential(NativeCode.NativeCredential ncred)
        {
            Flags = ncred.Flags;
            TargetName = ncred.TargetName;
            Comment = ncred.Comment;
            try
            {
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                LastWritten = DateTime.FromFileTime((long)((ulong)ncred.LastWritten.dwHighDateTime << 32 | (uint)ncred.LastWritten.dwLowDateTime));
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
            }
            catch (ArgumentOutOfRangeException)
            { }


            var CredentialBlobSize = ncred.CredentialBlobSize;
            if (ncred.CredentialBlobSize >= 2)
            {
                CredentialBlob = Marshal.PtrToStringUni(ncred.CredentialBlob, (int)ncred.CredentialBlobSize / 2);
            }
            Persistance = (Persistance)ncred.Persist;
            var AttributeCount = ncred.AttributeCount;
            if (AttributeCount > 0)
            {
                try
                {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                    var formatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                    var attribSize = Marshal.SizeOf(typeof(NativeCode.NativeCredentialAttribute));
                    Attributes = new Dictionary<string, object>();
                    byte[] rawData = new byte[AttributeCount * attribSize];
                    var buffer = Marshal.AllocHGlobal(attribSize);
                    Marshal.Copy(ncred.Attributes, rawData, 0, (int)AttributeCount * attribSize);
                    for (int i = 0; i < AttributeCount; i++)
                    {
                        Marshal.Copy(rawData, i * attribSize, buffer, attribSize);
                        var attr = (NativeCode.NativeCredentialAttribute)Marshal.PtrToStructure(buffer,
                         typeof(NativeCode.NativeCredentialAttribute));
                        var key = attr.Keyword;
                        var val = new byte[attr.ValueSize];
                        Marshal.Copy(attr.Value, val, 0, (int)attr.ValueSize);
                        using var stream = new MemoryStream(val, false);
                        Attributes.Add(key, formatter.Deserialize(stream));
                    }
                    Marshal.FreeHGlobal(buffer);
                    rawData = null;
                }
                catch
                {

                }
            }
            TargetAlias = ncred.TargetAlias;
            UserName = ncred.UserName;
            Type = (CredentialType)ncred.Type;
        }

        public Credential(NetworkCredential credential)
        {
            CredentialBlob = credential.Password;
            UserName = string.IsNullOrWhiteSpace(credential.Domain) ? credential.UserName : credential.Domain + "\\" + credential.UserName;
            Attributes = null;
            Comment = null;
            TargetAlias = null;
            Type = CredentialType.Generic;
            Persistance = Persistance.Session;
        }

        public Credential(ICredential credential)
        {
            CredentialBlob = credential.CredentialBlob;
            UserName = credential.UserName;
            if (credential.Attributes?.Count > 0)
            {
                Attributes = new Dictionary<string, object>();
                foreach (var a in credential.Attributes)
                {
                    Attributes.Add(a);
                }
            }
            Comment = credential.Comment;
            TargetAlias = null;
            Type = credential.Type;
            Persistance = credential.Persistance;
        }

        public Credential(string target, CredentialType type)
        {
            Type = type;
            TargetName = target;
        }

        public NetworkCredential ToNetworkCredential()
        {
            if (!string.IsNullOrEmpty(UserName))
            {
                var userBuilder = new StringBuilder(UserName.Length + 2);
                var domainBuilder = new StringBuilder(UserName.Length + 2);

                var returnCode = NativeCode.CredUIParseUserName(UserName, userBuilder, userBuilder.Capacity, domainBuilder, domainBuilder.Capacity);
                var lastError = Marshal.GetLastWin32Error();

                //assuming invalid account name to be not meeting condition for CredUIParseUserName
                //"The name must be in UPN or down-level format, or a certificate"
                if (returnCode == NativeCode.CredentialUIReturnCodes.InvalidAccountName)
                {
                    userBuilder.Append(UserName);
                }
                else if (returnCode != 0)
                {
                    throw new CredentialAPIException($"Unable to Parse UserName", "CredUIParseUserName", lastError);
                }

                return new NetworkCredential(userBuilder.ToString(), CredentialBlob, domainBuilder.ToString());
            }
            else
            {
                return new NetworkCredential(UserName, CredentialBlob);
            }
        }

        public bool SaveCredential(bool AllowBlankPassword = false)
        {
            nint buffer = default;
            GCHandle pinned = default;

            if (!string.IsNullOrEmpty(Comment) && Encoding.Unicode.GetBytes(Comment).Length > 256)
                throw new ArgumentException("Comment can't be more than 256 bytes long", "Comment");

            if (string.IsNullOrEmpty(TargetName))
                throw new ArgumentNullException("TargetName", "TargetName can't be Null or Empty");
            else if (TargetName.Length > 32767)
                throw new ArgumentNullException("TargetName can't be more than 32kB", "TargetName");

            if (!AllowBlankPassword && string.IsNullOrEmpty(CredentialBlob))
                throw new ArgumentNullException("CredentialBlob", "CredentialBlob can't be Null or Empty");

            NativeCode.NativeCredential ncred = new NativeCode.NativeCredential
            {
                Comment = Comment,
                TargetAlias = null,
                Type = (uint)Type,
                Persist = (uint)Persistance,
                UserName = UserName,
                TargetName = TargetName,
                CredentialBlobSize = (uint)Encoding.Unicode.GetBytes(CredentialBlob).Length
            };
            if (ncred.CredentialBlobSize > MaxCredentialBlobSize)
                throw new ArgumentException($"Credential can't be more than {MaxCredentialBlobSize} bytes long", "CredentialBlob");

            ncred.CredentialBlob = Marshal.StringToCoTaskMemUni(CredentialBlob);
            if (LastWritten != DateTime.MinValue)
            {
                var fileTime = LastWritten.ToFileTimeUtc();
                ncred.LastWritten.dwLowDateTime = (int)(fileTime & 0xFFFFFFFFL);
                ncred.LastWritten.dwHighDateTime = (int)(fileTime >> 32 & 0xFFFFFFFFL);
            }

            NativeCode.NativeCredentialAttribute[] nativeAttribs = null;
            try
            {
                if (Attributes == null || Attributes.Count == 0)
                {
                    ncred.AttributeCount = 0;
                    ncred.Attributes = nint.Zero;
                }
                else
                {
                    if (Attributes.Count > 64)
                        throw new ArgumentException("Credentials can't have more than 64 Attributes!!");

                    ncred.AttributeCount = (uint)Attributes.Count;
                    nativeAttribs = new NativeCode.NativeCredentialAttribute[Attributes.Count];
                    var attribSize = Marshal.SizeOf(typeof(NativeCode.NativeCredentialAttribute));
                    byte[] rawData = new byte[Attributes.Count * attribSize];
                    buffer = Marshal.AllocHGlobal(attribSize);

#pragma warning disable SYSLIB0011 // Type or member is obsolete
                    var formatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011 // Type or member is obsolete

                    var i = 0;
                    foreach (var a in Attributes)
                    {
                        if (a.Key.Length > 256)
                            throw new ArgumentException($"Attribute names can't be more than 256 bytes long. Error with key:{a.Key}", a.Key);
                        if (a.Value == null)
                            throw new ArgumentNullException(a.Key, $"Attribute value cant'be null. Error with key:{a.Key}");
                        if (!a.Value.GetType().IsSerializable)
                            throw new ArgumentException($"Attribute value must be Serializable. Error with key:{a.Key}", a.Key);

                        using var stream = new MemoryStream();
                        formatter.Serialize(stream, a.Value);
                        var value = stream.ToArray();

                        if (value.Length > 256)
                            throw new ArgumentException($"Attribute values can't be more than 256 bytes long after serialization. Error with Value for key:{a.Key}", a.Key);

                        var attrib = new NativeCode.NativeCredentialAttribute
                        {
                            Keyword = a.Key,
                            ValueSize = (uint)value.Length
                        };

                        attrib.Value = Marshal.AllocHGlobal(value.Length);
                        Marshal.Copy(value, 0, attrib.Value, value.Length);
                        nativeAttribs[i] = attrib;

                        Marshal.StructureToPtr(attrib, buffer, false);
                        Marshal.Copy(buffer, rawData, i * attribSize, attribSize);
                        i++;
                    }
                    pinned = GCHandle.Alloc(rawData, GCHandleType.Pinned);
                    ncred.Attributes = pinned.AddrOfPinnedObject();
                }
                // Write the info into the CredMan storage.

                if (NativeCode.CredWrite(ref ncred, 0))
                {
                    return true;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new CredentialAPIException($"Unable to Save Credential", "CredWrite", lastError);
                }
            }

            finally
            {
                if (ncred.CredentialBlob != default)
                    Marshal.FreeCoTaskMem(ncred.CredentialBlob);
                if (nativeAttribs != null)
                {
                    foreach (var a in nativeAttribs)
                    {
                        if (a.Value != default)
                            Marshal.FreeHGlobal(a.Value);
                    }
                    if (pinned.IsAllocated)
                        pinned.Free();
                    if (buffer != default)
                        Marshal.FreeHGlobal(buffer);
                }
            }
        }

        public bool RemoveCredential()
        {
            // Make the API call using the P/Invoke signature
            var isSuccess = NativeCode.CredDelete(TargetName, (uint)Type, 0);

            if (isSuccess)
                return true;

            int lastError = Marshal.GetLastWin32Error();
            throw new CredentialAPIException($"Unable to Delete Credential", "CredDelete", lastError);
        }
    }

}

