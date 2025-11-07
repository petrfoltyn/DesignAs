#pragma once

// Concrete properties
struct ConcreteProperties {
    double fcd;      // [Pa] design compressive strength (negative)
    double epsC2;    // [-] strain at peak stress
    double epsCu;    // [-] ultimate compressive strain
};

// Steel properties
struct SteelProperties {
    double fyd;      // [Pa] design yield strength
    double Es;       // [Pa] elastic modulus
    double epsUd;    // [-] ultimate tensile strain
};

// Section geometry
struct SectionGeometry {
    double b;        // [m] width
    double h;        // [m] height
    double d1;       // [m] top reinforcement cover from top edge
    double d2;       // [m] bottom reinforcement cover from bottom edge
};

// Design loads
struct DesignLoads {
    double N;        // [N] axial force (+ tension, - compression)
    double M;        // [Nm] bending moment
};
