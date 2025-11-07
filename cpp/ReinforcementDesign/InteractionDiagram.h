#pragma once
#include "MaterialProperties.h"
#include "ConcreteIntegration.h"
#include "ConcreteIntegrationFast.h"  // Use analytical integration
#include "SteelStress.h"
#include <vector>
#include <string>
#include <cmath>
#include <fstream>
#include <iostream>

// Single point on interaction diagram
struct DiagramPoint {
    std::string name;
    double epsTop;       // [per mille] strain at top fiber
    double epsBot;       // [per mille] strain at bottom fiber
    double epsS1;        // [per mille] strain in top reinforcement
    double epsS2;        // [per mille] strain in bottom reinforcement
    double sigS1;        // [MPa] stress in top reinforcement
    double sigS2;        // [MPa] stress in bottom reinforcement
    double N;            // [kN] axial force
    double M;            // [kNm] moment
    double Fc;           // [kN] concrete force
    double Mc;           // [kNm] concrete moment
    double Fs1;          // [kN] top steel force
    double Fs2;          // [kN] bottom steel force
    double As1;          // [cm^2] top reinforcement area
    double As2;          // [cm^2] bottom reinforcement area
};

// Interaction diagram generator
class InteractionDiagram {
private:
    SectionGeometry geom;
    ConcreteProperties concrete;
    SteelProperties steel;
    double As1_input;  // [m^2] top reinforcement (for diagram with reinforcement)
    double As2_input;  // [m^2] bottom reinforcement (for diagram with reinforcement)

    // Calculate single point given strain distribution parameters
    // Using strain model: eps(y) = k*y + q, where y is from bottom (y=0 at bottom, y=h at top)
    DiagramPoint CalculatePoint(const std::string& name, double epsTop, double epsBot) {
        DiagramPoint pt;
        pt.name = name;
        pt.epsTop = epsTop * 1000.0;  // convert to per mille
        pt.epsBot = epsBot * 1000.0;

        // Concrete forces - USE FAST ANALYTICAL METHOD
        ConcreteForces cf = ConcreteIntegrationFast::CalculateForce(
            epsTop, epsBot, geom.b, geom.h, concrete
        );
        pt.Fc = cf.Fc / 1000.0;  // N to kN
        pt.Mc = cf.Mc / 1000.0;  // Nm to kNm

        // Strain in reinforcement layers
        double y1_from_top = geom.d1;  // top reinforcement distance from top edge
        double y2_from_top = geom.h - geom.d2;  // bottom reinforcement distance from top edge

        pt.epsS1 = (epsTop + (epsBot - epsTop) * y1_from_top / geom.h) * 1000.0;  // per mille
        pt.epsS2 = (epsTop + (epsBot - epsTop) * y2_from_top / geom.h) * 1000.0;

        // Stress in reinforcement
        double epsS1_abs = pt.epsS1 / 1000.0;
        double epsS2_abs = pt.epsS2 / 1000.0;
        pt.sigS1 = SteelStress::CalculateStress(epsS1_abs, steel) / 1e6;  // Pa to MPa
        pt.sigS2 = SteelStress::CalculateStress(epsS2_abs, steel) / 1e6;

        // Steel forces
        pt.Fs1 = As1_input * pt.sigS1 * 1e6 / 1000.0;  // kN
        pt.Fs2 = As2_input * pt.sigS2 * 1e6 / 1000.0;  // kN

        // Total forces
        pt.N = pt.Fc + pt.Fs1 + pt.Fs2;

        // Moments from steel (about centroid)
        double y1_center = geom.d1 - geom.h / 2.0;  // distance from centroid
        double y2_center = geom.d2 - geom.h / 2.0;
        double Ms1 = pt.Fs1 * (-y1_center);  // kNm
        double Ms2 = pt.Fs2 * (-y2_center);
        pt.M = pt.Mc + Ms1 + Ms2;

        // Reinforcement areas
        pt.As1 = As1_input * 10000.0;  // m^2 to cm^2
        pt.As2 = As2_input * 10000.0;

        return pt;
    }

    // Linear interpolation between two points
    std::vector<DiagramPoint> InterpolateBetween(const DiagramPoint& p1, const DiagramPoint& p2, int numPoints) {
        std::vector<DiagramPoint> points;

        for (int i = 1; i < numPoints; i++) {
            double t = static_cast<double>(i) / numPoints;

            // Interpolate strain values
            double epsTop = p1.epsTop / 1000.0 + t * (p2.epsTop / 1000.0 - p1.epsTop / 1000.0);
            double epsBot = p1.epsBot / 1000.0 + t * (p2.epsBot / 1000.0 - p1.epsBot / 1000.0);

            std::string interpName = "Interp_" + p1.name + "_to_" + p2.name + "_" + std::to_string(i);
            points.push_back(CalculatePoint(interpName, epsTop, epsBot));
        }

        return points;
    }

public:
    InteractionDiagram(const SectionGeometry& g, const ConcreteProperties& c,
                      const SteelProperties& s, double as1 = 0.0, double as2 = 0.0)
        : geom(g), concrete(c), steel(s), As1_input(as1), As2_input(as2) {}

    // Generate interaction diagram with characteristic points and densification
    std::vector<DiagramPoint> Generate(int pointsBetween = 10) {
        std::vector<DiagramPoint> allPoints;

        // Calculate yield strain
        double epsYd = steel.fyd / steel.Es;
        double epsCu = concrete.epsCu;
        double epsC2 = concrete.epsC2;
        double epsUd = steel.epsUd;

        // CHARACTERISTIC POINTS (following C# implementation)

        // POINT 1: Pure compression (epsTop = epsBottom = epsCu)
        DiagramPoint p1 = CalculatePoint("P1_PureCompression", epsCu, epsCu);
        allPoints.push_back(p1);

        // POINT 2: Top = epsCu, Bottom = epsC2
        DiagramPoint p2 = CalculatePoint("P2_Top_epsCu_Bot_epsC2", epsCu, epsC2);
        auto interp12 = InterpolateBetween(p1, p2, pointsBetween);
        allPoints.insert(allPoints.end(), interp12.begin(), interp12.end());
        allPoints.push_back(p2);

        // POINT 2b: Top = epsCu, Bottom = 0
        DiagramPoint p2b = CalculatePoint("P2b_Top_epsCu_Bot_0", epsCu, 0.0);
        auto interp2_2b = InterpolateBetween(p2, p2b, pointsBetween);
        allPoints.insert(allPoints.end(), interp2_2b.begin(), interp2_2b.end());
        allPoints.push_back(p2b);

        // POINT 3: Top = epsCu, Bottom steel yields (epsS2 = epsYd)
        // Calculate epsBot such that epsS2 = epsYd
        double y2_from_top = geom.h - geom.d2;
        double epsBot_p3 = epsYd - (epsYd - epsCu) * (geom.h - y2_from_top) / y2_from_top;
        DiagramPoint p3 = CalculatePoint("P3_Top_epsCu_S2_yield", epsCu, epsBot_p3);
        auto interp2b_3 = InterpolateBetween(p2b, p3, pointsBetween);
        allPoints.insert(allPoints.end(), interp2b_3.begin(), interp2b_3.end());
        allPoints.push_back(p3);

        // POINT 4: Top = epsCu, Bottom steel ultimate (epsS2 = epsUd)
        double epsBot_p4 = epsUd - (epsUd - epsCu) * (geom.h - y2_from_top) / y2_from_top;
        DiagramPoint p4 = CalculatePoint("P4_Top_epsCu_S2_ultimate", epsCu, epsBot_p4);
        auto interp3_4 = InterpolateBetween(p3, p4, pointsBetween);
        allPoints.insert(allPoints.end(), interp3_4.begin(), interp3_4.end());
        allPoints.push_back(p4);

        // POINT 5: Top = epsC2, Bottom steel ultimate (epsS2 = epsUd)
        double epsBot_p5 = epsUd - (epsUd - epsC2) * (geom.h - y2_from_top) / y2_from_top;
        DiagramPoint p5 = CalculatePoint("P5_Top_epsC2_S2_ultimate", epsC2, epsBot_p5);
        auto interp4_5 = InterpolateBetween(p4, p5, pointsBetween);
        allPoints.insert(allPoints.end(), interp4_5.begin(), interp4_5.end());
        allPoints.push_back(p5);

        // POINT 6: Top = 0, Bottom steel ultimate (epsS2 = epsUd)
        double epsBot_p6 = epsUd - (epsUd - 0.0) * (geom.h - y2_from_top) / y2_from_top;
        DiagramPoint p6 = CalculatePoint("P6_Top_0_S2_ultimate", 0.0, epsBot_p6);
        auto interp5_6 = InterpolateBetween(p5, p6, pointsBetween);
        allPoints.insert(allPoints.end(), interp5_6.begin(), interp5_6.end());
        allPoints.push_back(p6);

        // POINT 7: Both reinforcement layers yield/ultimate
        // Top steel yields (epsS1 = epsYd), Bottom steel ultimate (epsS2 = epsUd)
        double y1_from_top = geom.d1;
        double k_p7 = (epsYd - epsUd) / (y1_from_top - y2_from_top);
        double epsTop_p7 = epsYd - k_p7 * y1_from_top;
        double epsBot_p7 = epsTop_p7 + k_p7 * geom.h;
        DiagramPoint p7 = CalculatePoint("P7_S1_yield_S2_ultimate", epsTop_p7, epsBot_p7);
        auto interp6_7 = InterpolateBetween(p6, p7, pointsBetween);
        allPoints.insert(allPoints.end(), interp6_7.begin(), interp6_7.end());
        allPoints.push_back(p7);

        // POINT 8: Pure tension (epsTop = epsBottom = epsUd)
        DiagramPoint p8 = CalculatePoint("P8_PureTension", epsUd, epsUd);
        auto interp7_8 = InterpolateBetween(p7, p8, pointsBetween);
        allPoints.insert(allPoints.end(), interp7_8.begin(), interp7_8.end());
        allPoints.push_back(p8);

        return allPoints;
    }

    // Export diagram to CSV file
    static void ExportToCSV(const std::vector<DiagramPoint>& points, const std::string& filename) {
        std::ofstream file(filename);
        if (!file.is_open()) {
            std::cerr << "Error: Could not open file " << filename << " for writing\n";
            return;
        }

        // Header
        file << "Name,epsTop[o/oo],epsBot[o/oo],epsS1[o/oo],epsS2[o/oo],"
             << "sigS1[MPa],sigS2[MPa],N[kN],M[kNm],Fc[kN],Mc[kNm],"
             << "Fs1[kN],Fs2[kN],As1[cm^2],As2[cm^2]\n";

        // Data rows
        for (const auto& pt : points) {
            file << pt.name << ","
                 << pt.epsTop << "," << pt.epsBot << ","
                 << pt.epsS1 << "," << pt.epsS2 << ","
                 << pt.sigS1 << "," << pt.sigS2 << ","
                 << pt.N << "," << pt.M << ","
                 << pt.Fc << "," << pt.Mc << ","
                 << pt.Fs1 << "," << pt.Fs2 << ","
                 << pt.As1 << "," << pt.As2 << "\n";
        }

        file.close();
        std::cout << "Diagram exported to: " << filename << "\n";
        std::cout << "Total points: " << points.size() << "\n";
    }
};
