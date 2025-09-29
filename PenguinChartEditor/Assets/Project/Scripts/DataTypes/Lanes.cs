// Yes, this is jank. Oops

public interface IFiveFretLane { }

public struct FiveFretGreen : IFiveFretLane { }
public struct FiveFretRed : IFiveFretLane { }
public struct FiveFretYellow : IFiveFretLane { }
public struct FiveFretBlue : IFiveFretLane { }
public struct FiveFretOrange : IFiveFretLane { }
public struct FiveFretOpen : IFiveFretLane { }

public interface IFourLaneDrumLane { }

public struct FourLaneDrumRed : IFourLaneDrumLane { }
public struct FourLaneDrumYellow : IFourLaneDrumLane { }
public struct FourLaneDrumBlue : IFourLaneDrumLane { }
public struct FourLaneDrumGreen : IFourLaneDrumLane { }
public struct FourLaneDrumKick : IFourLaneDrumLane { }

public interface IGHLLane { }

public struct GHLWhite1 : IGHLLane { }
public struct GHLWhite2 : IGHLLane { }
public struct GHLWhite3 : IGHLLane { }
public struct GHLBlack1 : IGHLLane { }
public struct GHLBlack2 : IGHLLane { }
public struct GHLBlack3 : IGHLLane { }
public struct GHLOpen : IGHLLane {}