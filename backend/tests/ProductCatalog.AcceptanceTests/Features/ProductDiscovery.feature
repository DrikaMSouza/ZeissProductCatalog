Feature: Product discovery
  As a store operator
  I want to find products by name and by stock level
  So that I can answer customers and plan replenishment

  Background:
    Given the following products exist
      | Name                   | Stock |
      | Progressive Lens 1.60  | 40    |
      | Single Vision Lens     | 8     |
      | Titanium Frame Classic | 0     |
      | Polarized Sunglasses   | 3     |

  Scenario: Searching by part of the name ignores letter case
    When I search for products named "LENS"
    Then I see exactly these products
      | Name                  |
      | Progressive Lens 1.60 |
      | Single Vision Lens    |

  Scenario: Finding products that need to be replenished
    When I list the products with stock between 0 and 5
    Then I see exactly these products
      | Name                   |
      | Titanium Frame Classic |
      | Polarized Sunglasses   |
    And every listed product shows its stock
