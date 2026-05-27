// NebulaGE Tooling — build.zig
// Zig tooling: asset pipeline, shader compiler, project generator, nebula CLI
//
// Prerequisites: zig >= 0.13.0  (ziglang.org)

const std = @import("std");

pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{});
    const optimize = b.standardOptimizeOption(.{});

    // ── nebula CLI ───────────────────────────────────────────
    const nebula_cli = b.addExecutable(.{
        .name = "nebula",
        .root_source_file = b.path("src/main.zig"),
        .target = target,
        .optimize = optimize,
    });
    b.installArtifact(nebula_cli);

    // ── nebula-shader-compiler ───────────────────────────────
    // Compiles GLSL → SPIR-V using glslangValidator or shaderc
    // const shader_compiler = b.addExecutable(.{
    //     .name = "nebula-shaderc",
    //     .root_source_file = b.path("src/shaderc/main.zig"),
    //     .target = target,
    //     .optimize = optimize,
    // });
    // b.installArtifact(shader_compiler);

    // ── Run step ────────────────────────────────────────────
    const run_cmd = b.addRunArtifact(nebula_cli);
    run_cmd.step.dependOn(b.getInstallStep());
    if (b.args) |args| run_cmd.addArgs(args);

    const run_step = b.step("run", "Run the nebula CLI");
    run_step.dependOn(&run_cmd.step);

    // ── Tests ────────────────────────────────────────────────
    const unit_tests = b.addTest(.{
        .root_source_file = b.path("src/main.zig"),
        .target = target,
        .optimize = optimize,
    });
    const run_tests = b.addRunArtifact(unit_tests);
    const test_step = b.step("test", "Run unit tests");
    test_step.dependOn(&run_tests.step);
}
