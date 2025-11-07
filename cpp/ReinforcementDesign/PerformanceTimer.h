#pragma once
#include <chrono>
#include <string>
#include <iostream>
#include <iomanip>
#include <vector>
#include <fstream>

// Performance measurement result
struct TimingResult {
    std::string operation;
    double time_ms;
    std::string details;
};

// Performance timer and logger
class PerformanceTimer {
private:
    std::chrono::high_resolution_clock::time_point start_time;
    std::string operation_name;
    std::vector<TimingResult> results;
    bool auto_log;

public:
    PerformanceTimer(bool enable_auto_log = true) : auto_log(enable_auto_log) {}

    // Start timing an operation
    void Start(const std::string& op_name) {
        operation_name = op_name;
        start_time = std::chrono::high_resolution_clock::now();
    }

    // Stop timing and record result
    double Stop(const std::string& details = "") {
        auto end_time = std::chrono::high_resolution_clock::now();
        auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end_time - start_time);
        double time_ms = duration.count() / 1000.0;

        TimingResult result;
        result.operation = operation_name;
        result.time_ms = time_ms;
        result.details = details;
        results.push_back(result);

        if (auto_log) {
            std::cout << "[PERF] " << operation_name << ": "
                     << std::fixed << std::setprecision(3) << time_ms << " ms";
            if (!details.empty()) {
                std::cout << " (" << details << ")";
            }
            std::cout << "\n";
        }

        return time_ms;
    }

    // Get all results
    const std::vector<TimingResult>& GetResults() const {
        return results;
    }

    // Print summary
    void PrintSummary() const {
        std::cout << "\n==========================================================\n";
        std::cout << "  PERFORMANCE SUMMARY\n";
        std::cout << "==========================================================\n\n";

        double total_time = 0.0;
        for (const auto& result : results) {
            std::cout << std::setw(40) << std::left << result.operation << ": "
                     << std::setw(10) << std::right << std::fixed << std::setprecision(3)
                     << result.time_ms << " ms";
            if (!result.details.empty()) {
                std::cout << "  (" << result.details << ")";
            }
            std::cout << "\n";
            total_time += result.time_ms;
        }

        std::cout << "\n" << std::string(60, '-') << "\n";
        std::cout << std::setw(40) << std::left << "TOTAL TIME" << ": "
                 << std::setw(10) << std::right << std::fixed << std::setprecision(3)
                 << total_time << " ms\n";
        std::cout << "==========================================================\n";
    }

    // Export to CSV
    void ExportToCSV(const std::string& filename) const {
        std::ofstream file(filename);
        if (!file.is_open()) {
            std::cerr << "Error: Could not open " << filename << "\n";
            return;
        }

        // Header
        file << "Operation,Time_ms,Details\n";

        // Data
        for (const auto& result : results) {
            file << result.operation << ","
                 << std::fixed << std::setprecision(6) << result.time_ms << ","
                 << result.details << "\n";
        }

        file.close();
        std::cout << "Performance data exported to: " << filename << "\n";
    }

    // Analyze and suggest optimizations
    void Analyze() const {
        std::cout << "\n==========================================================\n";
        std::cout << "  PERFORMANCE ANALYSIS\n";
        std::cout << "==========================================================\n\n";

        // Calculate statistics
        double total_time = 0.0;
        double max_time = 0.0;
        std::string slowest_operation;

        for (const auto& result : results) {
            total_time += result.time_ms;
            if (result.time_ms > max_time) {
                max_time = result.time_ms;
                slowest_operation = result.operation;
            }
        }

        std::cout << "Total operations: " << results.size() << "\n";
        std::cout << "Total time: " << std::fixed << std::setprecision(3) << total_time << " ms\n";
        std::cout << "Average time per operation: " << (total_time / results.size()) << " ms\n";
        std::cout << "Slowest operation: " << slowest_operation << " (" << max_time << " ms)\n\n";

        // Percentage breakdown
        std::cout << "Time breakdown by percentage:\n";
        for (const auto& result : results) {
            double percentage = (result.time_ms / total_time) * 100.0;
            if (percentage > 1.0) {  // Only show operations > 1%
                std::cout << "  " << std::setw(40) << std::left << result.operation
                         << ": " << std::setw(6) << std::right << std::fixed
                         << std::setprecision(2) << percentage << " %\n";
            }
        }

        std::cout << "\n";

        // Optimization suggestions
        std::cout << "OPTIMIZATION SUGGESTIONS:\n";
        std::cout << "-------------------------\n";

        for (const auto& result : results) {
            double percentage = (result.time_ms / total_time) * 100.0;

            if (result.operation.find("ConcreteIntegration") != std::string::npos && percentage > 20) {
                std::cout << "- Concrete integration takes " << percentage
                         << "% of time. Consider:\n";
                std::cout << "  * Use analytical formulas instead of numerical integration\n";
                std::cout << "  * Reduce number of integration segments (currently 100)\n";
                std::cout << "  * Use lookup tables for common strain states\n\n";
            }

            if (result.operation.find("InteractionDiagram") != std::string::npos && percentage > 30) {
                std::cout << "- Diagram generation takes " << percentage
                         << "% of time. Consider:\n";
                std::cout << "  * Cache diagrams for common geometries\n";
                std::cout << "  * Reduce interpolation density (fewer points)\n";
                std::cout << "  * Generate diagram in parallel threads\n\n";
            }

            if (result.operation.find("Design") != std::string::npos && result.time_ms > 5.0) {
                std::cout << "- Design operation is slow (" << result.time_ms
                         << " ms). Consider:\n";
                std::cout << "  * Better bracketing algorithm (binary search)\n";
                std::cout << "  * Cache recent lookups\n";
                std::cout << "  * Use spatial indexing (R-tree) for diagram points\n\n";
            }
        }

        // General suggestions
        std::cout << "GENERAL RECOMMENDATIONS:\n";
        std::cout << "-------------------------\n";

        if (total_time < 100) {
            std::cout << "- Total time is already very fast (< 100 ms)\n";
            std::cout << "- Focus on code readability over micro-optimizations\n";
        } else if (total_time < 1000) {
            std::cout << "- Performance is acceptable for interactive use\n";
            std::cout << "- Consider optimizations only if processing many cases\n";
        } else {
            std::cout << "- Performance needs improvement for interactive use\n";
            std::cout << "- Prioritize optimization of slowest operations\n";
        }

        std::cout << "\n";
    }
};

// RAII-style timer for automatic start/stop
class ScopedTimer {
private:
    PerformanceTimer& timer;
    std::string operation;
    std::string details;

public:
    ScopedTimer(PerformanceTimer& t, const std::string& op, const std::string& det = "")
        : timer(t), operation(op), details(det) {
        timer.Start(operation);
    }

    ~ScopedTimer() {
        timer.Stop(details);
    }
};
