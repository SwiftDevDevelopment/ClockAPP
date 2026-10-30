"""Benchmark the published clock on a disposable macOS runner, not a user's profile."""
import ctypes as C
import json
import platform
import statistics
import subprocess
import sys
import time
from pathlib import Path

cf = C.CDLL('/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation')
cg = C.CDLL('/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics')
cf.CFStringCreateWithCString.argtypes = [C.c_void_p, C.c_char_p, C.c_uint32]
cf.CFStringCreateWithCString.restype = C.c_void_p
cf.CFArrayGetCount.argtypes = [C.c_void_p]
cf.CFArrayGetCount.restype = C.c_long
cf.CFArrayGetValueAtIndex.argtypes = [C.c_void_p, C.c_long]
cf.CFArrayGetValueAtIndex.restype = C.c_void_p
cf.CFDictionaryGetValue.argtypes = [C.c_void_p, C.c_void_p]
cf.CFDictionaryGetValue.restype = C.c_void_p
cf.CFNumberGetValue.argtypes = [C.c_void_p, C.c_int, C.c_void_p]
cf.CFBooleanGetValue.argtypes = [C.c_void_p]
cf.CFBooleanGetValue.restype = C.c_bool
cf.CFRelease.argtypes = [C.c_void_p]
cg.CGWindowListCopyWindowInfo.argtypes = [C.c_uint32, C.c_uint32]
cg.CGWindowListCopyWindowInfo.restype = C.c_void_p

keys = {name: cf.CFStringCreateWithCString(None, name.encode(), 0x08000100)
        for name in ['kCGWindowOwnerPID', 'kCGWindowIsOnscreen', 'kCGWindowBounds', 'Width', 'Height']}


def value(dictionary, key):
    return cf.CFDictionaryGetValue(dictionary, keys[key])


def visible_window(pid):
    windows = cg.CGWindowListCopyWindowInfo(0, 0)
    if not windows:
        return False
    try:
        for i in range(cf.CFArrayGetCount(windows)):
            window = cf.CFArrayGetValueAtIndex(windows, i)
            owner = value(window, 'kCGWindowOwnerPID')
            number = C.c_int32()
            if not owner or not cf.CFNumberGetValue(owner, 3, C.byref(number)) or number.value != pid:
                continue
            onscreen = value(window, 'kCGWindowIsOnscreen')
            bounds = value(window, 'kCGWindowBounds')
            if not onscreen or not cf.CFBooleanGetValue(onscreen) or not bounds:
                continue
            width, height = C.c_double(), C.c_double()
            w, h = value(bounds, 'Width'), value(bounds, 'Height')
            if w and h and cf.CFNumberGetValue(w, 13, C.byref(width)) and cf.CFNumberGetValue(h, 13, C.byref(height)):
                if width.value >= 50 and height.value >= 20:
                    return True
        return False
    finally:
        cf.CFRelease(windows)


def snapshot(pid):
    fields = subprocess.check_output(['ps', '-p', str(pid), '-o', 'rss=', '-o', 'time='], text=True).split()
    if len(fields) != 2:
        raise RuntimeError('Cannot read process RSS/CPU counters')
    parts = fields[1].split(':')
    cpu = sum(float(part) * (60 ** i) for i, part in enumerate(reversed(parts)))
    return int(fields[0]) / 1024, cpu


def run(binary, output, seconds, number):
    settings = Path.home() / 'Library/Application Support/SimpleClockOverlay.CrossPlatform/settings.json'
    settings.parent.mkdir(parents=True, exist_ok=True)
    settings.write_text(json.dumps({'ShowSeconds': seconds, 'AutoUpdates': False,
                                   'FontSize': 32, 'BackgroundOpacity': 80, 'AlwaysOnTop': True}))
    log_path = output / f'process-{number}.log'
    with log_path.open('w') as log:
        began = time.monotonic()
        process = subprocess.Popen([str(binary)], stdout=log, stderr=log)
        try:
            while not visible_window(process.pid):
                if process.poll() is not None:
                    raise RuntimeError(f'Clock exited before a window appeared; see {log_path}')
                if time.monotonic() - began > 30:
                    raise RuntimeError('No visible clock window after 30 seconds; not recording a headless benchmark')
                time.sleep(0.05)
            startup = time.monotonic() - began
            time.sleep(10)
            _, before = snapshot(process.pid)
            sample_start = time.monotonic()
            memory = []
            for _ in range(30):
                time.sleep(1)
                rss, _ = snapshot(process.pid)
                memory.append(rss)
            _, after = snapshot(process.pid)
            elapsed = time.monotonic() - sample_start
            if not visible_window(process.pid):
                raise RuntimeError('Clock window was no longer onscreen at the end of the sample')
            return {'seconds': seconds, 'run': number, 'startup_to_visible_seconds': round(startup, 3),
                    'sample_seconds': round(elapsed, 3), 'cpu_seconds': round(after - before, 3),
                    'cpu_percent_one_core': round((after - before) / elapsed * 100, 3),
                    'rss_mib_median': round(statistics.median(memory), 1),
                    'rss_mib_peak': round(max(memory), 1), 'visible_window_verified': True}
        finally:
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()


def main():
    binary, output = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve()
    output.mkdir(parents=True, exist_ok=True)
    result = {'platform': platform.platform(), 'architecture': platform.machine(),
              'os_version': subprocess.check_output(['sw_vers', '-productVersion'], text=True).strip(),
              'cpu_model': subprocess.check_output(['sysctl', '-n', 'machdep.cpu.brand_string'], text=True).strip(),
              'logical_cpus': int(subprocess.check_output(['sysctl', '-n', 'hw.logicalcpu'], text=True)),
              'release': '2.0.0-preview.2', 'packaging_revision': 'mac.1', 'warmup_seconds': 10,
              'method': 'Published binary; GUI window verified through CoreGraphics; ps RSS and process CPU time. 3 x 30-second samples per mode. Hosted-runner results, not physical-Mac guarantees.',
              'samples': []}
    # Alternate modes so neither consistently receives the coldest or warmest host.
    for number, seconds in enumerate([True, False, True, False, True, False], 1):
        sample = run(binary, output, seconds, number)
        result['samples'].append(sample)
        (output / 'results.json').write_text(json.dumps(result, indent=2))
        print(json.dumps(sample), flush=True)
    summary = []
    for seconds in [True, False]:
        rows = [s for s in result['samples'] if s['seconds'] == seconds]
        summary.append({'seconds': seconds,
                        'cpu_percent_one_core_median': round(statistics.median(s['cpu_percent_one_core'] for s in rows), 3),
                        'rss_mib_median': round(statistics.median(s['rss_mib_median'] for s in rows), 1),
                        'subsequent_launch_seconds_median': round(statistics.median(s['startup_to_visible_seconds'] for s in rows if s['run'] != 1), 3)})
    result['summary'] = summary
    result['first_launch_seconds'] = result['samples'][0]['startup_to_visible_seconds']
    (output / 'results.json').write_text(json.dumps(result, indent=2))
    lines = [f"# Mac benchmark — {result['architecture']}", '', result['method'], '',
             f"macOS {result['os_version']} · {result['cpu_model']} · {result['logical_cpus']} logical CPUs", '',
             '| Seconds | CPU (% of one core) | Resident memory (MiB) | Subsequent launch to visible (s) |',
             '| --- | ---: | ---: | ---: |']
    for row in summary:
        lines.append(f"| {'On' if row['seconds'] else 'Off'} | {row['cpu_percent_one_core_median']} | {row['rss_mib_median']} | {row['subsequent_launch_seconds_median']} |")
    lines += ['', f"First launch to a visible clock: {result['first_launch_seconds']} seconds. OS file caches are not controlled.", '',
              'CPU uses process time divided by elapsed time; 100% is one fully occupied core. RSS is resident memory, not private allocations. Samples include a 10-second warmup. Settings, architecture, renderer, runner load and virtualization affect results.']
    (output / 'summary.md').write_text('\n'.join(lines) + '\n')
    print('\n'.join(lines), flush=True)


if __name__ == '__main__':
    main()

