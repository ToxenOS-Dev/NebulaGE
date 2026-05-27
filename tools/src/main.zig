// nebula — NebulaGE CLI tool
// Commands: create, build, run, new-script, compile-shaders, ...

const std = @import("std");

pub fn main() !void {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    defer _ = gpa.deinit();
    const allocator = gpa.allocator();

    const args = try std.process.argsAlloc(allocator);
    defer std.process.argsFree(allocator, args);

    if (args.len < 2) {
        printHelp();
        return;
    }

    const command = args[1];

    if (std.mem.eql(u8, command, "create")) {
        // TODO: implement project creation
        std.debug.print("nebula create — not yet implemented\n", .{});
    } else if (std.mem.eql(u8, command, "build")) {
        // TODO: implement project build
        std.debug.print("nebula build — not yet implemented\n", .{});
    } else if (std.mem.eql(u8, command, "run")) {
        // TODO: implement project run
        std.debug.print("nebula run — not yet implemented\n", .{});
    } else if (std.mem.eql(u8, command, "compile-shaders")) {
        // TODO: implement GLSL → SPIR-V compilation
        std.debug.print("nebula compile-shaders — not yet implemented\n", .{});
    } else {
        std.debug.print("Unknown command: {s}\n\n", .{command});
        printHelp();
    }
}

fn printHelp() void {
    std.debug.print(
        \\nebula — NebulaGE CLI
        \\
        \\Usage:
        \\  nebula <command> [options]
        \\
        \\Commands:
        \\  create <name>        Create a new NebulaGE project
        \\  build                Build the current project
        \\  run                  Build and run the current project
        \\  compile-shaders      Compile GLSL shaders to SPIR-V
        \\
        \\Options:
        \\  --help               Show this message
        \\
    , .{});
}
