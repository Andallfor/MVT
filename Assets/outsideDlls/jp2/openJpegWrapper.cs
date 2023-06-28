using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class openJpegWrapper {
    public static int[] requestTerrain(string file, Vector2Int start, Vector2Int end, uint res, uint quality) {
        // TODO: add error checking
        // TODO: test across a lot of systems to ensure endianess is respected!
        IntPtr dparam = openjpeg_openjp2_opj_dparameters_t_new();
        openjpeg_openjp2_opj_dparameters_t_set_cod_format(dparam, 2); // jp2
        openjpeg_openjp2_opj_dparameters_t_set_cp_layer(dparam, quality);
        openjpeg_openjp2_opj_dparameters_t_set_cp_reduce(dparam, res);
        openjpeg_openjp2_opj_dparameters_t_set_DA_x0(dparam, (uint) start.x);
        openjpeg_openjp2_opj_dparameters_t_set_DA_y0(dparam, (uint) start.y);
        openjpeg_openjp2_opj_dparameters_t_set_DA_x1(dparam, (uint) end.x);
        openjpeg_openjp2_opj_dparameters_t_set_DA_y1(dparam, (uint) end.y);

        byte[] fname = Encoding.ASCII.GetBytes(file);
        IntPtr stream = openjpeg_openjp2_opj_stream_create_default_file_stream(fname, (uint) fname.Length, true);
        IntPtr codec = openjpeg_openjp2_opj_create_decompress(2); // jp2

        openjpeg_openjp2_opj_setup_decoder(codec, dparam);
        openjpeg_openjp2_opj_codec_set_threads(codec, 32);
        openjpeg_openjp2_opj_read_header(stream, codec, out IntPtr raw);
        openjpeg_openjp2_opj_set_decode_area(codec, raw, (uint) start.x, (uint) start.y, (uint) end.x, (uint) end.y);

        openjpeg_openjp2_opj_decode(codec, stream, raw);

        openjpeg_openjp2_opj_end_decompress(codec, stream);
        
        IntPtr imgc = openjpeg_openjp2_opj_image_t_get_comps_by_index(raw, 0);
        uint nrows = openjpeg_openjp2_opj_image_comp_t_get_h(imgc);
        uint ncols = openjpeg_openjp2_opj_image_comp_t_get_w(imgc);

        int power = (int) Math.Pow(2, res);
        if (nrows * power != end.y - start.y || ncols * power != end.x - start.x) Debug.LogWarning("OpenJpeg: Output height or width does not match desired!");

        long len = 4 * nrows * ncols;
        byte[] data = new byte[len];

        unsafe {
            fixed (byte* arrStart = &data[0]) {
                System.Buffer.MemoryCopy((void*) openjpeg_openjp2_opj_image_comp_t_get_data(imgc), arrStart, len, len);
            }
        }

        int[] formatted = new int[nrows * ncols];
        Buffer.BlockCopy(data, 0, formatted, 0, data.Length);

        openjpeg_openjp2_opj_destroy_codec(codec);
        openjpeg_openjp2_opj_stream_destroy(stream);
        openjpeg_openjp2_opj_image_t_destroy(raw);

        return formatted;
    }

    private const string lib = "OpenJpegDotNetNative";
    private const CallingConvention ccon = CallingConvention.Cdecl;

    [DllImport(lib, CallingConvention = ccon)] private static extern IntPtr openjpeg_openjp2_opj_dparameters_t_new();
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_dparameters_t_set_cod_format(IntPtr parameters, int value);
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_dparameters_t_set_cp_layer(IntPtr parameters, uint value);
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_dparameters_t_set_cp_reduce(IntPtr parameters, uint value);
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_dparameters_t_set_DA_x0(IntPtr parameters, uint value);
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_dparameters_t_set_DA_y0(IntPtr parameters, uint value);
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_dparameters_t_set_DA_x1(IntPtr parameters, uint value);
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_dparameters_t_set_DA_y1(IntPtr parameters, uint value);
    [DllImport(lib, CallingConvention = ccon)] private static extern IntPtr openjpeg_openjp2_opj_stream_create_default_file_stream(byte[] fname, uint fname_len, bool p_is_read_stream);
    [DllImport(lib, CallingConvention = ccon)] private static extern IntPtr openjpeg_openjp2_opj_create_decompress(int format);
    [DllImport(lib, CallingConvention = ccon)] private static extern IntPtr openjpeg_openjp2_opj_image_t_get_comps_by_index(IntPtr image, uint index);
    [DllImport(lib, CallingConvention = ccon)] private static extern uint openjpeg_openjp2_opj_image_comp_t_get_h(IntPtr comp);
    [DllImport(lib, CallingConvention = ccon)] private static extern uint openjpeg_openjp2_opj_image_comp_t_get_w(IntPtr comp);
    [DllImport(lib, CallingConvention = ccon)] private static extern IntPtr openjpeg_openjp2_opj_image_comp_t_get_data(IntPtr comp);
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_destroy_codec(IntPtr p_codec);
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_stream_destroy(IntPtr p_stream);
    [DllImport(lib, CallingConvention = ccon)] private static extern void openjpeg_openjp2_opj_image_t_destroy(IntPtr image);
    [DllImport(lib, CallingConvention = ccon)] [return: MarshalAs(UnmanagedType.U1)] private static extern bool openjpeg_openjp2_opj_setup_decoder(IntPtr p_codec, IntPtr parameters);
    [DllImport(lib, CallingConvention = ccon)] [return: MarshalAs(UnmanagedType.U1)] private static extern bool openjpeg_openjp2_opj_codec_set_threads(IntPtr p_codec, int num_threads);
    [DllImport(lib, CallingConvention = ccon)] [return: MarshalAs(UnmanagedType.U1)] private static extern bool openjpeg_openjp2_opj_read_header(IntPtr p_stream, IntPtr p_codec, out IntPtr p_image);
    [DllImport(lib, CallingConvention = ccon)] [return: MarshalAs(UnmanagedType.U1)] private static extern bool openjpeg_openjp2_opj_set_decode_area(IntPtr p_codec, IntPtr p_image, uint sx, uint sy, uint ex, uint ey);
    [DllImport(lib, CallingConvention = ccon)] [return: MarshalAs(UnmanagedType.U1)] private static extern bool openjpeg_openjp2_opj_decode(IntPtr p_codec, IntPtr p_stream, IntPtr p_image);
    [DllImport(lib, CallingConvention = ccon)] [return: MarshalAs(UnmanagedType.U1)] private static extern bool openjpeg_openjp2_opj_end_decompress(IntPtr p_codec, IntPtr p_stream);
}

public struct decompTerrainData {
    public int[] data;
    public Vector2Int size, srcSize, start, end;
    public geographic offset;
    public int rlevel, res;
    public bool isSmall;
    public float stepSizeGeoX, stepSizeGeoY, fileLengthX, fileLengthY;
}